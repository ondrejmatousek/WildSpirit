using DesignStore.Domain.Catalog;
using DesignStore.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace DesignStore.E2E.Tests.Scenarios;

[Collection(E2ETestCollection.Name)]
public sealed class CriticalSmokeTests
{
    [Fact(Timeout = 120_000)]
    public Task Admin_can_login_and_logout() => RunIsolatedAsync(nameof(Admin_can_login_and_logout), async (fixture, page) =>
    {
        await fixture.LoginAsAdminAsync(page);
        await page.GetByTestId("admin-logout-button").ClickAsync(new LocatorClickOptions { Force = true });
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/?$"));
        var cookies = await page.Context.CookiesAsync();
        Assert.DoesNotContain(cookies, cookie => cookie.Name.Contains("Identity.Application", StringComparison.OrdinalIgnoreCase));
    });

    [Fact(Timeout = 120_000)]
    public Task Admin_creates_product_without_images() => RunIsolatedAsync(nameof(Admin_creates_product_without_images), async (fixture, page) =>
    {
        var prefix = "E2E-NOIMG";
        var sku = $"{prefix}-{Guid.NewGuid():N}"[..28].ToUpperInvariant();
        var slug = sku.ToLowerInvariant();
        try
        {
            await fixture.LoginAsAdminAsync(page);
            await CreateAdminProductAsync(page, sku, slug, "E2E produkt bez fotky", readyMade: true);

            await Assertions.Expect(page.GetByText("Produkt byl")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("product-sku")).ToHaveValueAsync(sku);
            await Assertions.Expect(page.GetByText("Produktové fotografie")).ToBeVisibleAsync();
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Admin_creates_product_with_one_uploaded_image() => RunIsolatedAsync(nameof(Admin_creates_product_with_one_uploaded_image), async (fixture, page) =>
    {
        var prefix = "E2E-IMG";
        var sku = $"{prefix}-{Guid.NewGuid():N}"[..28].ToUpperInvariant();
        var slug = sku.ToLowerInvariant();
        var imagePath = Path.Combine(fixture.RepoRoot, "src", "DesignStore.Web", "wwwroot", "images", "brand", "hero-meadow-sunset-v3.png");
        Assert.True(File.Exists(imagePath), $"Expected E2E upload fixture image to exist: {imagePath}");
        try
        {
            await fixture.LoginAsAdminAsync(page);
            await CreateAdminProductAsync(page, sku, slug, "E2E produkt s fotkou", readyMade: true);
            var productId = ProductIdFromCurrentUrl(page);

            await TimedStepAsync("SetInputFilesAsync on product image file input", () =>
                page.GetByTestId("product-image-files").SetInputFilesAsync(imagePath, new LocatorSetInputFilesOptions { Timeout = 10_000 }));
            await TimedStepAsync("Fill image alt text", () =>
                page.GetByTestId("product-image-alt").FillAsync("E2E produktová fotografie", new LocatorFillOptions { Timeout = 5_000 }));
            await TimedStepAsync("Verify upload form has selected file before submit", async () =>
            {
                var selectedFiles = await page.GetByTestId("product-image-files").EvaluateAsync<int>("input => input.files.length");
                Assert.Equal(1, selectedFiles);
            }, TimeSpan.FromSeconds(5));
            await TimedStepAsync("Submit product image upload form", async () =>
            {
                await ClickAndIgnoreNavigationTimeoutAsync(page.GetByTestId("product-image-upload"));
            }, TimeSpan.FromSeconds(5));

            await TimedStepAsync("Verify uploaded image was persisted", async () =>
                await WaitUntilAsync(async () => await fixture.CountImagesAsync(productId) == 1), TimeSpan.FromSeconds(25));
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Published_product_appears_in_public_catalog() => RunIsolatedAsync(nameof(Published_product_appears_in_public_catalog), async (fixture, page) =>
    {
        var prefix = "E2E-CATALOG";
        var slug = $"e2e-catalog-{Guid.NewGuid():N}";
        var name = "E2E veřejný produkt";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 3);

            await GoToAsync(page, "/cs-CZ/Catalog");
            await Assertions.Expect(page.GetByTestId("public-product-grid")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='public-product-card'] h2", new PageLocatorOptions { HasTextString = name })).ToBeVisibleAsync();
            var productHref = await page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(name) }).GetAttributeAsync("href");
            Assert.False(string.IsNullOrWhiteSpace(productHref));
            Assert.Contains(slug, productHref, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task In_stock_product_can_be_added_to_cart() => RunIsolatedAsync(nameof(In_stock_product_can_be_added_to_cart), async (fixture, page) =>
    {
        var prefix = "E2E-CART";
        var slug = $"e2e-cart-{Guid.NewGuid():N}";
        var name = "E2E košíkový produkt";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5);

            await GoToAsync(page, $"/cs-CZ/product/{slug}");
            await Assertions.Expect(page.GetByTestId("add-to-cart-button")).ToBeVisibleAsync();
            var productId = await page.Locator("input[name='ProductId']").InputValueAsync();
            var token = await page.GetByTestId("add-to-cart-form").Locator("input[name='__RequestVerificationToken']").InputValueAsync();
            await page.EvaluateAsync(
                "async data => { const form = new FormData(); form.append('__RequestVerificationToken', data.token); form.append('ProductId', data.productId); form.append('Quantity', '1'); const response = await fetch('/cs-CZ/cart/add', { method: 'POST', body: form, credentials: 'include' }); if (!response.ok) throw new Error(`Add to cart failed: ${response.status}`); }",
                new { productId, token });
            await GoToAsync(page, "/cs-CZ/cart");
            await Assertions.Expect(page.GetByTestId("cart-line")).ToHaveCountAsync(1);
            await Assertions.Expect(page.GetByText(name)).ToBeVisibleAsync();
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Made_to_order_product_allows_inquiry_but_cannot_be_added_to_cart() => RunIsolatedAsync(nameof(Made_to_order_product_allows_inquiry_but_cannot_be_added_to_cart), async (fixture, page) =>
    {
        var prefix = "E2E-MTO";
        var slug = $"e2e-made-to-order-{Guid.NewGuid():N}";
        var name = "E2E zakázkový produkt";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.MadeToOrder, 0);

            await GoToAsync(page, $"/cs-CZ/product/{slug}");
            await Assertions.Expect(page.GetByTestId("add-to-cart-button")).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByTestId("inquiry-link")).ToBeVisibleAsync();
            var inquiryHref = await page.GetByTestId("inquiry-link").GetAttributeAsync("href");
            Assert.False(string.IsNullOrWhiteSpace(inquiryHref));
            await GoToAsync(page, inquiryHref);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/inquiry/.+"));
            await Assertions.Expect(page.GetByText("Popt")).ToBeVisibleAsync();
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    private static async Task CreateAdminProductAsync(IPage page, string sku, string slug, string name, bool readyMade)
    {
        await GoToAsync(page, "/cs-CZ/Admin/Products/Create");
        await page.GetByTestId("product-sku").FillAsync(sku);
        await page.GetByTestId("product-category").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.GetByTestId("product-collection").SelectOptionAsync(new SelectOptionValue { Index = 1 });

        if (readyMade)
        {
            await page.GetByTestId("product-base-price").FillAsync("2490");
            await page.GetByTestId("product-stock").FillAsync("4");
            await page.GetByTestId("product-availability").SelectOptionAsync("1");
        }
        else
        {
            await page.Locator("[data-product-mode='custom']").CheckAsync();
            await page.GetByTestId("product-starting-price").FillAsync("3490");
            await page.GetByTestId("product-production-days").FillAsync("21");
            await page.GetByTestId("product-availability").SelectOptionAsync("2");
            await page.GetByTestId("product-can-be-made-to-order").CheckAsync(new LocatorCheckOptions { Force = true });
        }

        await page.GetByTestId("product-published").CheckAsync(new LocatorCheckOptions { Force = true });
        await FillInputAsync(page, "Translations[0].Name", name);
        await FillInputAsync(page, "Translations[0].Slug", slug);
        await FillInputAsync(page, "Translations[0].ShortDescription", "Krátký E2E popis produktu.");
        await FillInputAsync(page, "Translations[0].LongDescription", "Dlouhý E2E popis produktu bez HTML.");
        await FillInputAsync(page, "Translations[1].Name", $"{name} EN");
        await FillInputAsync(page, "Translations[1].Slug", $"{slug}-en");
        await FillInputAsync(page, "Translations[2].Name", $"{name} DE");
        await FillInputAsync(page, "Translations[2].Slug", $"{slug}-de");
        await page.GetByTestId("product-save").ClickAsync(new LocatorClickOptions { Force = true });
    }

    private static async Task FillInputAsync(IPage page, string name, string value)
    {
        await page.Locator($"[name='{name}']").EvaluateAsync(
            "(element, value) => { element.value = value; element.dispatchEvent(new Event('input', { bubbles: true })); element.dispatchEvent(new Event('change', { bubbles: true })); }",
            value);
    }

    private static async Task GoToAsync(IPage page, string url)
    {
        try
        {
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Commit, Timeout = 10_000 });
        }
        catch (TimeoutException) when (page.Url.Contains(url.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
        {
            // The response committed and the DOM is usable, but Playwright did not observe the final navigation signal.
        }
    }

    private static Guid ProductIdFromCurrentUrl(IPage page)
    {
        var lastSegment = new Uri(page.Url).Segments.Last().TrimEnd('/');
        Assert.True(Guid.TryParse(lastSegment, out var productId), $"Expected product edit URL to end with a product id, but current URL was '{page.Url}'.");
        return productId;
    }

    private static async Task TimedStepAsync(string stepName, Func<Task> action, TimeSpan? timeout = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(10);
        try
        {
            await action().WaitAsync(limit);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException($"Upload step timed out after {limit.TotalSeconds:0} seconds: {stepName}.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Upload step failed: {stepName}.", ex);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(250);
        }
        Assert.Fail("Condition was not met before timeout.");
    }

    private static async Task ClickAndIgnoreNavigationTimeoutAsync(ILocator locator)
    {
        try
        {
            await locator.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
        }
    }

    private static async Task RunIsolatedAsync(string name, Func<E2ETestFixture, IPage, Task> action)
    {
        var fixture = new E2ETestFixture();
        await fixture.InitializeAsync();
        try
        {
            await fixture.RunWithArtifactsAsync(name, page => action(fixture, page));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
