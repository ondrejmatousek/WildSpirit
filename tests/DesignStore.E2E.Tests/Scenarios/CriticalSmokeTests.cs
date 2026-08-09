using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.EntityFrameworkCore;
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
            await page.GetByTestId("add-to-cart-button").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 10_000 });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/cart/?$"));
            await Assertions.Expect(page.GetByTestId("cart-line")).ToHaveCountAsync(1);
            await Assertions.Expect(page.GetByTestId("cart-line").Filter(new() { HasTextString = name })).ToBeVisibleAsync();
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

    [Fact(Timeout = 120_000)]
    public Task Customer_can_complete_checkout() => RunIsolatedAsync(nameof(Customer_can_complete_checkout), async (fixture, page) =>
    {
        var prefix = "E2E-CHECKOUT";
        var slug = $"e2e-checkout-{Guid.NewGuid():N}";
        var name = "E2E checkout produkt";
        var emailPrefix = $"e2e.checkout.{Guid.NewGuid():N}";
        var email = $"{emailPrefix}@wildspirit.local";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5);
            await AddProductToCartAsync(page, slug, name);
            await GoToAsync(page, "/cs-CZ/checkout");
            await FillValidCheckoutAsync(page, email);
            await page.GetByTestId("checkout-shipping-pickup").CheckAsync(new LocatorCheckOptions { Force = true });
            await page.GetByTestId("checkout-payment-bank").CheckAsync(new LocatorCheckOptions { Force = true });
            await page.GetByTestId("checkout-submit").ClickAsync();

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/checkout/confirmation/.+"));
            await Assertions.Expect(page.GetByText("Objednávka")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(email)).ToHaveCountAsync(0);
        }
        finally
        {
            await fixture.CleanupOrdersAsync(emailPrefix);
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Checkout_creates_exactly_one_order() => RunIsolatedAsync(nameof(Checkout_creates_exactly_one_order), async (fixture, page) =>
    {
        var prefix = "E2E-ONEORDER";
        var slug = $"e2e-one-order-{Guid.NewGuid():N}";
        var sku = $"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant();
        var name = "E2E jedna objednávka";
        var emailPrefix = $"e2e.oneorder.{Guid.NewGuid():N}";
        var email = $"{emailPrefix}@wildspirit.local";
        try
        {
            await fixture.SeedProductAsync(sku, slug, name, AvailabilityStatus.InStock, 5);
            await CompleteCheckoutAsync(page, slug, name, email, pickup: true);

            var orders = await fixture.OrdersByEmailPrefixAsync(emailPrefix);
            var order = Assert.Single(orders);
            var item = Assert.Single(order.Items);
            Assert.Equal(sku, item.Sku);
            Assert.Equal(1, item.Quantity);
            Assert.Equal(2490m, item.UnitPrice);
            Assert.Equal(2490m, order.Subtotal);
            Assert.Equal(0m, order.ShippingTotal);
            Assert.Equal(2490m, order.Total);
        }
        finally
        {
            await fixture.CleanupOrdersAsync(emailPrefix);
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Admin_can_see_created_order() => RunIsolatedAsync(nameof(Admin_can_see_created_order), async (fixture, page) =>
    {
        var prefix = "E2E-ADMINORDER";
        var slug = $"e2e-admin-order-{Guid.NewGuid():N}";
        var name = "E2E objednávka v adminu";
        var emailPrefix = $"e2e.adminorder.{Guid.NewGuid():N}";
        var email = $"{emailPrefix}@wildspirit.local";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5);
            await CompleteCheckoutAsync(page, slug, name, email, pickup: true);
            var order = Assert.Single(await fixture.OrdersByEmailPrefixAsync(emailPrefix));

            await fixture.LoginAsAdminAsync(page);
            await GoToAsync(page, "/cs-CZ/Admin/Orders");
            await Assertions.Expect(page.GetByTestId("admin-order-row").Filter(new() { HasTextString = order.Number })).ToBeVisibleAsync();
            await page.GetByTestId("admin-order-link").Filter(new() { HasTextString = order.Number }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = order.Number })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-contact-email]")).ToContainTextAsync(email);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = name })).ToBeVisibleAsync();
        }
        finally
        {
            await fixture.CleanupOrdersAsync(emailPrefix);
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Customer_can_change_cart_quantity() => RunIsolatedAsync(nameof(Customer_can_change_cart_quantity), async (fixture, page) =>
    {
        var prefix = "E2E-QTY";
        var slug = $"e2e-qty-{Guid.NewGuid():N}";
        var name = "E2E změna množství";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5);
            await AddProductToCartAsync(page, slug, name);
            await page.GetByTestId("cart-line-quantity").FillAsync("3");
            await page.GetByTestId("cart-line-update").ClickAsync();

            await Assertions.Expect(page.GetByTestId("cart-line-quantity")).ToHaveValueAsync("3");
            await Assertions.Expect(page.GetByTestId("cart-line-total")).ToContainTextAsync(new Regex("7\\s*470"));
            await Assertions.Expect(page.GetByTestId("cart-total")).ToContainTextAsync(new Regex("7\\s*470"));
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Customer_can_remove_product_from_cart() => RunIsolatedAsync(nameof(Customer_can_remove_product_from_cart), async (fixture, page) =>
    {
        var prefix = "E2E-REMOVE";
        var slug = $"e2e-remove-{Guid.NewGuid():N}";
        var name = "E2E odebrání z košíku";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5);
            await AddProductToCartAsync(page, slug, name);
            await page.GetByTestId("cart-line-remove").ClickAsync();

            await Assertions.Expect(page.GetByTestId("cart-empty")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("cart-total")).ToHaveCountAsync(0);
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Unpublished_product_is_not_visible_publicly() => RunIsolatedAsync(nameof(Unpublished_product_is_not_visible_publicly), async (fixture, page) =>
    {
        var prefix = "E2E-HIDDEN";
        var slug = $"e2e-hidden-{Guid.NewGuid():N}";
        var name = "E2E neveřejný produkt";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5, isPublished: false);
            await GoToAsync(page, "/cs-CZ/Catalog");
            await Assertions.Expect(page.GetByTestId("public-product-card").Filter(new() { HasTextString = name })).ToHaveCountAsync(0);
            Assert.Equal(404, await GetStatusAsync(fixture, $"/cs-CZ/product/{slug}"));
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Out_of_stock_product_cannot_be_added_to_cart() => RunIsolatedAsync(nameof(Out_of_stock_product_cannot_be_added_to_cart), async (fixture, page) =>
    {
        var prefix = "E2E-OOS";
        var slug = $"e2e-oos-{Guid.NewGuid():N}";
        var name = "E2E vyprodaný produkt";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 0);
            await GoToAsync(page, $"/cs-CZ/product/{slug}");
            await Assertions.Expect(page.GetByTestId("add-to-cart-button")).ToHaveCountAsync(0);
            await GoToAsync(page, "/cs-CZ/cart");
            await Assertions.Expect(page.GetByTestId("cart-empty")).ToBeVisibleAsync();
        }
        finally
        {
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Anonymous_user_cannot_access_admin() => RunIsolatedAsync(nameof(Anonymous_user_cannot_access_admin), async (_, page) =>
    {
        await GoToAsync(page, "/cs-CZ/Admin/Orders");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/Account/Login.*"));
        await Assertions.Expect(page.GetByTestId("login-form")).ToBeVisibleAsync();
    });

    [Fact(Timeout = 120_000)]
    public Task Invalid_checkout_cannot_create_order() => RunIsolatedAsync(nameof(Invalid_checkout_cannot_create_order), async (fixture, page) =>
    {
        var prefix = "E2E-BADCHECKOUT";
        var slug = $"e2e-bad-checkout-{Guid.NewGuid():N}";
        var name = "E2E neplatná pokladna";
        var emailPrefix = $"e2e.badcheckout.{Guid.NewGuid():N}";
        var email = $"{emailPrefix}@wildspirit.local";
        try
        {
            await fixture.SeedProductAsync($"{prefix}-{Guid.NewGuid():N}"[..30].ToUpperInvariant(), slug, name, AvailabilityStatus.InStock, 5);
            await AddProductToCartAsync(page, slug, name);
            await GoToAsync(page, "/cs-CZ/checkout");
            await page.GetByTestId("checkout-email").FillAsync(email);
            await page.GetByTestId("checkout-submit").ClickAsync();

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/checkout/?$"));
            Assert.Empty(await fixture.OrdersByEmailPrefixAsync(emailPrefix));
        }
        finally
        {
            await fixture.CleanupOrdersAsync(emailPrefix);
            await fixture.CleanupProductsAsync(prefix);
        }
    });

    [Fact(Timeout = 120_000)]
    public Task Non_existing_product_returns_404() => RunIsolatedAsync(nameof(Non_existing_product_returns_404), async (fixture, _) =>
    {
        Assert.Equal(404, await GetStatusAsync(fixture, $"/cs-CZ/product/e2e-missing-{Guid.NewGuid():N}"));
    });

    private static async Task CompleteCheckoutAsync(IPage page, string slug, string productName, string email, bool pickup)
    {
        await AddProductToCartAsync(page, slug, productName);
        await GoToAsync(page, "/cs-CZ/checkout");
        await FillValidCheckoutAsync(page, email);
        if (pickup) await page.GetByTestId("checkout-shipping-pickup").CheckAsync(new LocatorCheckOptions { Force = true });
        else await page.GetByTestId("checkout-shipping-standard").CheckAsync(new LocatorCheckOptions { Force = true });
        await page.GetByTestId("checkout-payment-bank").CheckAsync(new LocatorCheckOptions { Force = true });
        await page.GetByTestId("checkout-submit").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/checkout/confirmation/.+"));
    }

    private static async Task AddProductToCartAsync(IPage page, string slug, string productName)
    {
        await GoToAsync(page, $"/cs-CZ/product/{slug}");
        await Assertions.Expect(page.GetByTestId("product-detail-title")).ToContainTextAsync(productName);
        await page.GetByTestId("add-to-cart-button").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 10_000 });
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/cs-CZ/cart/?$"));
        await Assertions.Expect(page.GetByTestId("cart-line")).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("cart-line").Filter(new() { HasTextString = productName })).ToBeVisibleAsync();
    }

    private static async Task FillValidCheckoutAsync(IPage page, string email)
    {
        await page.GetByTestId("checkout-email").FillAsync(email);
        await page.GetByTestId("checkout-billing-name").FillAsync("E2E Zákazník");
        await page.GetByTestId("checkout-billing-company").FillAsync("WildSpirit E2E");
        await page.GetByTestId("checkout-billing-street").FillAsync("Testovací 12");
        await page.GetByTestId("checkout-billing-city").FillAsync("Praha");
        await page.GetByTestId("checkout-billing-postal-code").FillAsync("14900");
        await page.GetByTestId("checkout-billing-country").FillAsync("CZ");
        await page.GetByTestId("checkout-billing-phone").FillAsync("774501987");
    }

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

    private static async Task<int> GetStatusAsync(E2ETestFixture fixture, string path)
    {
        using var client = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };
        using var response = await client.GetAsync(path);
        return (int)response.StatusCode;
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
