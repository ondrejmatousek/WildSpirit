using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Identity;
using DesignStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace DesignStore.E2E.Tests.Infrastructure;

public sealed class E2ETestFixture : IAsyncLifetime
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan TeardownTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ArtifactTimeout = TimeSpan.FromSeconds(3);
    private Process? _process;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly List<string> _serverOutput = [];
    private readonly List<string> _serverError = [];
    private readonly string _databaseName = $"DesignStoreE2E_{Guid.NewGuid():N}";
    private readonly int _port = GetFreePort();

    public string AdminEmail { get; } = "e2e.admin@wildspirit.local";
    public string AdminPassword { get; } = "WildSpirit.E2E.Admin.2026!";
    public string BaseUrl => $"http://127.0.0.1:{_port}";
    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser is not initialized.");
    public string RepoRoot { get; } = FindRepoRoot();
    public string ArtifactsRoot => Path.Combine(RepoRoot, "artifacts", "playwright");
    public string ConnectionString => (Environment.GetEnvironmentVariable("DESIGNSTORE_E2E_CONNECTION")
            ?? Environment.GetEnvironmentVariable("DESIGNSTORE_TEST_CONNECTION")
            ?? "Server=.\\SQLEXPRESS;Database={database};Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;Connect Timeout=30")
        .Replace("{database}", _databaseName, StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(ArtifactsRoot);
        await using (var services = CreateServices())
        {
            await MigrateAndSeedAsync(services);
        }

        StartApplication();
        await WaitUntilReadyAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await LaunchBrowserAsync(_playwright);
    }

    public async Task DisposeAsync()
    {
        await TryWithTimeoutAsync(
            async () =>
            {
                if (_browser is not null) await _browser.CloseAsync();
            },
            TeardownTimeout,
            "Closing Playwright browser during E2E teardown failed.");

        try
        {
            _playwright?.Dispose();
        }
        catch
        {
            // Teardown must never keep the next test from running.
        }

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TeardownTimeout);
            }
            catch
            {
                // The test runner process will continue; do not block suite teardown.
            }
        }

        await TryWithTimeoutAsync(
            async () =>
            {
                await using var services = CreateServices();
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureDeletedAsync();
            },
            TeardownTimeout,
            "Deleting E2E database during teardown failed.");
    }

    public async Task RunWithArtifactsAsync(string name, Func<IPage, Task> action)
    {
        var safeName = string.Concat(name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'));
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            Locale = "cs-CZ",
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(10_000);
        page.SetDefaultNavigationTimeout(10_000);
        var consoleErrors = new List<string>();
        var networkRequests = new List<string>();
        var failedRequests = new List<string>();
        page.Console += (_, message) =>
        {
            if (message.Type is "error" or "warning")
            {
                lock (consoleErrors)
                {
                    consoleErrors.Add($"{DateTime.UtcNow:O} [{message.Type}] {message.Text}");
                }
            }
        };
        page.Request += (_, request) =>
        {
            lock (networkRequests)
            {
                networkRequests.Add($"{DateTime.UtcNow:O} {request.Method} {request.Url}");
            }
        };
        page.RequestFailed += (_, request) =>
        {
            lock (failedRequests)
            {
                failedRequests.Add($"{DateTime.UtcNow:O} {request.Method} {request.Url} :: {request.Failure}");
            }
        };

        try
        {
            var testTask = action(page);
            var timeoutTask = Task.Delay(TestTimeout);
            if (await Task.WhenAny(testTask, timeoutTask) != testTask)
            {
                throw new TimeoutException($"E2E test '{name}' exceeded the per-test timeout of {TestTimeout.TotalSeconds:0} seconds.");
            }

            await testTask;
        }
        catch (Exception originalError)
        {
            var folder = Path.Combine(ArtifactsRoot, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{safeName}");
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, "failure-exception.txt"), originalError.ToString());
            await CaptureFailureArtifactsAsync(page, folder, consoleErrors, networkRequests, failedRequests, ServerOutputSnapshot(), ServerErrorSnapshot());
            ExceptionDispatchInfo.Capture(originalError).Throw();
            throw;
        }
        finally
        {
            await TryWithTimeoutAsync(
                () => page.CloseAsync(),
                TeardownTimeout,
                "Closing Playwright page during E2E teardown failed.");
            await TryWithTimeoutAsync(
                () => context.CloseAsync(),
                TeardownTimeout,
                "Closing Playwright context during E2E teardown failed.");
        }
    }

    public async Task LoginAsAdminAsync(IPage page)
    {
        await page.GotoAsync("/cs-CZ/Account/Login?returnUrl=%2Fcs-CZ%2FAdmin", new PageGotoOptions { WaitUntil = WaitUntilState.Commit });
        await page.GetByTestId("login-email").FillAsync(AdminEmail);
        await page.GetByTestId("login-password").FillAsync(AdminPassword);
        await page.GetByTestId("login-form").EvaluateAsync("form => form.requestSubmit()");
        await Expect(page.GetByText("Administrace")).ToBeVisibleAsync();
    }

    public ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task CleanupProductsAsync(string skuPrefix)
    {
        await using var db = CreateDb();
        var productIds = await db.Products
            .Where(p => p.Sku.StartsWith(skuPrefix))
            .Select(p => p.Id)
            .ToListAsync();

        if (productIds.Count == 0) return;

        await db.CartItems.Where(x => productIds.Contains(x.ProductId)).ExecuteDeleteAsync();
        await db.ProductInquiries.Where(x => productIds.Contains(x.ProductId)).ExecuteDeleteAsync();
        await db.InventoryMovements.Where(x => x.ProductId.HasValue && productIds.Contains(x.ProductId.Value)).ExecuteDeleteAsync();
        await db.ProductImages.Where(x => productIds.Contains(x.ProductId)).ExecuteDeleteAsync();
        await db.ProductSlugHistories.Where(x => productIds.Contains(x.ProductId)).ExecuteDeleteAsync();
        await db.ProductTranslations.Where(x => productIds.Contains(x.ProductId)).ExecuteDeleteAsync();
        await db.Products.Where(x => productIds.Contains(x.Id)).ExecuteDeleteAsync();
    }

    public async Task<Guid> SeedProductAsync(string sku, string slug, string name, AvailabilityStatus availability, int stockQuantity, bool isPublished = true)
    {
        await using var db = CreateDb();
        var categoryId = await db.Categories.OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstAsync();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            CategoryId = categoryId,
            BasePrice = availability == AvailabilityStatus.MadeToOrder ? 0 : 2490,
            StartingPrice = availability == AvailabilityStatus.MadeToOrder ? 3490 : null,
            AvailabilityStatus = availability,
            StockQuantity = stockQuantity,
            TrackInventory = true,
            CanBeMadeToOrder = availability == AvailabilityStatus.MadeToOrder,
            EstimatedProductionDays = availability == AvailabilityStatus.MadeToOrder ? 21 : null,
            IsPublished = isPublished,
            Translations =
            [
                Translation("cs-CZ", name, slug),
                Translation("en", $"{name} EN", $"{slug}-en"),
                Translation("de", $"{name} DE", $"{slug}-de")
            ]
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    public async Task<int> CountImagesAsync(string skuPrefix)
    {
        await using var db = CreateDb();
        return await db.ProductImages.CountAsync(image => image.Product.Sku.StartsWith(skuPrefix));
    }

    public async Task<int> CountImagesAsync(Guid productId)
    {
        await using var db = CreateDb();
        return await db.ProductImages.CountAsync(image => image.ProductId == productId);
    }

    private static ProductTranslation Translation(string culture, string name, string slug) => new()
    {
        Id = Guid.NewGuid(),
        Culture = culture,
        Name = name,
        Slug = slug,
        ShortDescription = $"Smoke popis {name}",
        LongDescription = $"Smoke dlouhý popis {name}",
        Story = $"Smoke příběh {name}",
        Materials = "Kůže, dřevo, mosaz",
        CareInstructions = "Čistit jemně.",
        ShippingInformation = "Odesíláme bezpečně.",
        SeoTitle = name,
        SeoDescription = $"SEO {name}"
    };

    private ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(ConnectionString));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = true;
            options.SignIn.RequireConfirmedEmail = true;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        return services.BuildServiceProvider();
    }

    private async Task MigrateAndSeedAsync(ServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!await roleManager.RoleExistsAsync("Administrator"))
        {
            var role = await roleManager.CreateAsync(new IdentityRole<Guid>("Administrator"));
            if (!role.Succeeded) throw new InvalidOperationException(string.Join("; ", role.Errors.Select(x => x.Description)));
        }

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true
            };
            var created = await userManager.CreateAsync(admin, AdminPassword);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
        }
        if (!await userManager.IsInRoleAsync(admin, "Administrator"))
        {
            var added = await userManager.AddToRoleAsync(admin, "Administrator");
            if (!added.Succeeded) throw new InvalidOperationException(string.Join("; ", added.Errors.Select(x => x.Description)));
        }

        if (!await db.Categories.AnyAsync())
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                DisplayOrder = 1,
                IsPublished = true,
                Translations =
                [
                    new() { Id = Guid.NewGuid(), Culture = "cs-CZ", Name = "E2E kabelky", Slug = "e2e-kabelky" },
                    new() { Id = Guid.NewGuid(), Culture = "en", Name = "E2E bags", Slug = "e2e-bags" },
                    new() { Id = Guid.NewGuid(), Culture = "de", Name = "E2E taschen", Slug = "e2e-taschen" }
                ]
            };
            db.Categories.Add(category);
        }

        if (!await db.Collections.AnyAsync())
        {
            db.Collections.Add(new Collection
            {
                Id = Guid.NewGuid(),
                DisplayOrder = 1,
                IsPublished = true,
                Translations =
                [
                    new() { Id = Guid.NewGuid(), Culture = "cs-CZ", Name = "E2E kolekce", Slug = "e2e-kolekce" }
                ]
            });
        }

        if (!await db.Materials.AnyAsync())
        {
            db.Materials.Add(new Material
            {
                Id = Guid.NewGuid(),
                Code = "E2E-LEATHER",
                IsActive = true,
                Translations =
                [
                    new() { Id = Guid.NewGuid(), Culture = "cs-CZ", Name = "E2E kůže" }
                ]
            });
        }

        await db.SaveChangesAsync();
    }

    private void StartApplication()
    {
        var webProject = Path.Combine(RepoRoot, "src", "DesignStore.Web", "DesignStore.Web.csproj");
        var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{webProject}\" --no-build --no-restore --no-launch-profile")
        {
            WorkingDirectory = RepoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "E2E";
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ConnectionStrings__DefaultConnection"] = ConnectionString;
        startInfo.Environment["Authentication__Google__ClientId"] = string.Empty;
        startInfo.Environment["Authentication__Google__ClientSecret"] = string.Empty;
        startInfo.Environment["Authentication__Facebook__AppId"] = string.Empty;
        startInfo.Environment["Authentication__Facebook__AppSecret"] = string.Empty;
        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start DesignStore.Web.");
        _process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null) return;
            lock (_serverOutput)
            {
                _serverOutput.Add($"{DateTime.UtcNow:O} {args.Data}");
            }
        };
        _process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null) return;
            lock (_serverError)
            {
                _serverError.Add($"{DateTime.UtcNow:O} {args.Data}");
            }
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private async Task WaitUntilReadyAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(45);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            if (_process?.HasExited == true)
            {
                var stdout = string.Join(Environment.NewLine, ServerOutputSnapshot());
                var stderr = string.Join(Environment.NewLine, ServerErrorSnapshot());
                throw new InvalidOperationException($"DesignStore.Web exited during E2E startup.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
            }

            try
            {
                var response = await client.GetAsync($"{BaseUrl}/health");
                if (response.IsSuccessStatusCode) return;
            }
            catch (Exception ex)
            {
                last = ex;
            }
            await Task.Delay(500);
        }
        throw new TimeoutException("DesignStore.Web did not become ready for E2E tests.", last);
    }

    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Channel = "chrome" });
        }
        catch
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
    }

    private static async Task CaptureFailureArtifactsAsync(IPage page, string folder, IReadOnlyCollection<string> consoleErrors, IReadOnlyCollection<string> networkRequests, IReadOnlyCollection<string> failedRequests, IReadOnlyCollection<string> serverOutput, IReadOnlyCollection<string> serverError)
    {
        await TryArtifactAsync(
            async () => await File.WriteAllTextAsync(Path.Combine(folder, "current-url.txt"), page.Url),
            Path.Combine(folder, "current-url-error.txt"));

        await TryArtifactAsync(
            async () =>
            {
                var messages = await page.Locator(".validation, .field-validation-error, [data-valmsg-for], .validation-summary-errors, .alert, .validation-summary-valid")
                    .EvaluateAllAsync<string[]>(
                        @"elements => elements
                            .filter(element => {
                                const style = window.getComputedStyle(element);
                                const rect = element.getBoundingClientRect();
                                return style.visibility !== 'hidden' && style.display !== 'none' && rect.width >= 0 && rect.height >= 0;
                            })
                            .map(element => element.innerText || element.textContent || '')
                            .map(text => text.trim())
                            .filter(Boolean)");
                await File.WriteAllLinesAsync(Path.Combine(folder, "visible-validation.txt"), messages);
            },
            Path.Combine(folder, "visible-validation-error.txt"));

        await TryArtifactAsync(
            async () =>
            {
                await File.WriteAllLinesAsync(Path.Combine(folder, "console-errors.txt"), consoleErrors);
                await File.WriteAllLinesAsync(Path.Combine(folder, "network-requests.txt"), networkRequests);
                await File.WriteAllLinesAsync(Path.Combine(folder, "failed-network-requests.txt"), failedRequests);
                await File.WriteAllLinesAsync(Path.Combine(folder, "server-output.txt"), serverOutput);
                await File.WriteAllLinesAsync(Path.Combine(folder, "server-error.txt"), serverError);
            },
            Path.Combine(folder, "browser-diagnostics-error.txt"));

        await TryArtifactAsync(
            async () => await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(folder, "failure.png"),
                FullPage = true,
                Timeout = 2_000
            }),
            Path.Combine(folder, "failure-screenshot-error.txt"));

        await TryArtifactAsync(
            async () =>
            {
                var html = await page.ContentAsync();
                await File.WriteAllTextAsync(Path.Combine(folder, "failure.html"), html);
            },
            Path.Combine(folder, "failure-html-error.txt"));
    }

    private string[] ServerOutputSnapshot()
    {
        lock (_serverOutput)
        {
            return [.. _serverOutput.TakeLast(300)];
        }
    }

    private string[] ServerErrorSnapshot()
    {
        lock (_serverError)
        {
            return [.. _serverError.TakeLast(300)];
        }
    }

    private static async Task TryArtifactAsync(Func<Task> action, string errorPath)
    {
        try
        {
            await action().WaitAsync(ArtifactTimeout);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(errorPath, error.ToString());
        }
    }

    private static async Task TryWithTimeoutAsync(Func<Task> action, TimeSpan timeout, string message)
    {
        try
        {
            await action().WaitAsync(timeout);
        }
        catch (Exception error)
        {
            Debug.WriteLine($"{message}{Environment.NewLine}{error}");
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DesignStore.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find DesignStore.slnx.");
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
}
