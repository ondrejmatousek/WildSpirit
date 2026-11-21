using DesignStore.Application.Commerce;
using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Commerce;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
namespace DesignStore.Tests.Infrastructure;

[Collection("PostgreSQL database")]
public sealed class CommerceSqlTests(PostgresDatabaseFixture database)
{
    static CommerceService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db) => new(db, [new BankTransferPaymentService()], new AddToCartRequestValidator(), new CheckoutRequestValidator());
    static CommerceService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db, string environment) => new(db, [new BankTransferPaymentService(), new TestPaymentService(new TestHostEnvironment { EnvironmentName = environment })], new AddToCartRequestValidator(), new CheckoutRequestValidator());
    static CheckoutRequest Checkout(string key) => new() { IdempotencyKey = key, Email = "buyer@example.test", BillingAddress = new AddressInput { FullName = "Test Buyer", Street = "Test 1", City = "Brno", PostalCode = "60200", CountryCode = "CZ" }, UseBillingAsShipping = true, ShippingMethod = "pickup", PaymentMethod = PaymentMethod.BankTransfer };
    async Task<Guid> Product(int stock, bool made = false)
    { await using var db = database.CreateDb(); var c = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Test", Slug = $"test-{Guid.NewGuid():N}", ShortDescription = "Test" }] }; var p = new Product { Category = c, Sku = $"C-{Guid.NewGuid():N}", BasePrice = 1210, VatRate = 21, IsPublished = true, AvailabilityStatus = AvailabilityStatus.InStock, StockQuantity = stock, CanBeMadeToOrder = made, Translations = [new ProductTranslation { Culture = "cs-CZ", Name = "Test produkt", Slug = $"product-{Guid.NewGuid():N}" }] }; db.Products.Add(p); await db.SaveChangesAsync(); return p.Id; }
    [Fact]
    public async Task Checkout_is_idempotent_and_cancellation_restores_stock_once()
    { var productId = await Product(2); var token = Guid.NewGuid().ToString("N"); await using (var db = database.CreateDb()) { var s = Service(db); await s.AddAsync(token, null, "cs-CZ", new(productId, null, 1, []), default); var first = await s.CheckoutAsync(token, null, Checkout("same-key"), default); var second = await s.CheckoutAsync(token, null, Checkout("same-key"), default); Assert.Equal(first.OrderId, second.OrderId); await s.CancelAsync(first.OrderId, "test", default); await s.CancelAsync(first.OrderId, "test", default); } await using (var verify = database.CreateDb()) { Assert.Equal(2, await verify.Products.Where(x => x.Id == productId).Select(x => x.StockQuantity).SingleAsync()); Assert.Equal(1, await verify.InventoryMovements.CountAsync(x => x.ProductId == productId && x.Type == InventoryMovementType.Return)); } }
    [Fact]
    public async Task Two_checkouts_cannot_buy_the_same_last_item_and_zero_switches_to_unavailable()
    { var productId = await Product(1); var t1 = Guid.NewGuid().ToString("N"); var t2 = Guid.NewGuid().ToString("N"); await using (var prep = database.CreateDb()) { var s = Service(prep); await s.AddAsync(t1, null, "cs-CZ", new(productId, null, 1, []), default); await s.AddAsync(t2, null, "cs-CZ", new(productId, null, 1, []), default); } var tasks = new[] { Run(t1, "race-1"), Run(t2, "race-2") }; var results = await Task.WhenAll(tasks); Assert.Equal(1, results.Count(x => x)); await using var verify = database.CreateDb(); var p = await verify.Products.SingleAsync(x => x.Id == productId); Assert.Equal(0, p.StockQuantity); Assert.Equal(AvailabilityStatus.Unavailable, p.AvailabilityStatus); Assert.Equal(1, await verify.Orders.CountAsync(x => x.Items.Any(i => i.ProductId == productId))); }
    async Task<bool> Run(string token, string key) { try { await using var db = database.CreateDb(); await Service(db).CheckoutAsync(token, null, Checkout(key), default); return true; } catch (ValidationException) { return false; } catch (DbUpdateException) { return false; } }
    [Fact] public async Task Made_to_order_product_cannot_be_added_to_cart() { var id = await Product(0, true); await using var db = database.CreateDb(); var p = await db.Products.SingleAsync(x => x.Id == id); p.AvailabilityStatus = AvailabilityStatus.MadeToOrder; await db.SaveChangesAsync(); await Assert.ThrowsAsync<ValidationException>(() => Service(db).AddAsync(Guid.NewGuid().ToString("N"), null, "cs-CZ", new(id, null, 1, []), default)); }
    [Fact]
    public async Task Cart_routine_update_discount_and_remove_saves_without_database_errors()
    {
        var productId = await Product(5);
        var token = Guid.NewGuid().ToString("N");
        await using var db = database.CreateDb();
        db.DiscountCodes.Add(new DiscountCode { Code = $"SAVE{Guid.NewGuid():N}"[..12].ToUpperInvariant(), Type = DiscountType.Percentage, Value = 10, MinimumOrderValue = 100, IsActive = true });
        await db.SaveChangesAsync();
        var discount = await db.DiscountCodes.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Select(x => x.Code).FirstAsync();
        var service = Service(db);

        await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
        var cart = await service.GetCartAsync(token, null, "cs-CZ", default);
        var itemId = cart.Items.Single().Id;

        await service.UpdateQuantityAsync(token, null, itemId, 3, default);
        await service.ApplyDiscountAsync(token, null, discount, default);
        var discounted = await service.GetCartAsync(token, null, "cs-CZ", default);

        Assert.Equal(3, discounted.Items.Single().Quantity);
        Assert.Equal(discount, discounted.DiscountCode);
        Assert.True(discounted.Discount > 0);

        await service.RemoveAsync(token, null, itemId, default);
        var empty = await service.GetCartAsync(token, null, "cs-CZ", default);

        Assert.Empty(empty.Items);
    }
    [Fact]
    public async Task Configured_product_can_go_from_cart_to_bank_transfer_order_with_standard_shipping()
    {
        var fixture = await ConfiguredProduct(stock: 3);
        var token = Guid.NewGuid().ToString("N");
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(fixture.ProductId, null, 2, [new CartConfigurationInput(fixture.OptionId, fixture.OptionValueId, null)]), default);
            var cart = await service.GetCartAsync(token, null, "cs-CZ", default);
            Assert.Single(cart.Items);
            Assert.Equal(2300m, cart.Subtotal);
            var checkout = Checkout($"bank-{Guid.NewGuid():N}");
            checkout.ShippingMethod = "standard";
            checkout.PaymentMethod = PaymentMethod.BankTransfer;

            var result = await service.CheckoutAsync(token, null, checkout, default);

            Assert.Equal(PaymentStatus.Pending, result.PaymentStatus);
            Assert.Equal(2550m, result.Total);
        }
        await using (var verify = database.CreateDb())
        {
            var product = await verify.Products.AsNoTracking().SingleAsync(x => x.Id == fixture.ProductId);
            var order = await verify.Orders.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Configuration).Include(x => x.Addresses).Include(x => x.Payments).Include(x => x.Shipments).SingleAsync(x => x.Items.Any(i => i.ProductId == fixture.ProductId));
            var item = order.Items.Single();
            var config = item.Configuration.Single();

            Assert.Equal(1, product.StockQuantity);
            Assert.Equal(OrderStatus.PendingPayment, order.Status);
            Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
            Assert.Equal("standard", order.ShippingMethod);
            Assert.Equal(2300m, order.Subtotal);
            Assert.Equal(250m, order.ShippingTotal);
            Assert.Equal(2550m, order.Total);
            Assert.Equal("Konfigurovaná kabelka", item.ProductName);
            Assert.Equal("CFG-SKU", item.Sku);
            Assert.Equal(1150m, item.UnitPrice);
            Assert.Equal(2, item.Quantity);
            Assert.Equal("Povrch", config.OptionName);
            Assert.Equal("Mosaz", config.Value);
            Assert.Equal(150m, config.PriceAdjustment);
            Assert.Equal(2, order.Addresses.Count);
            Assert.Equal(PaymentMethod.BankTransfer, order.Payments.Single().Method);
            Assert.Equal(PaymentStatus.Pending, order.Payments.Single().Status);
            Assert.StartsWith("VS-", order.Payments.Single().ProviderReference);
            Assert.Equal("standard", order.Shipments.Single().Service);
            Assert.Equal(250m, order.Shipments.Single().Price);
            Assert.False(await verify.CartItems.AsNoTracking().AnyAsync(x => x.ProductId == fixture.ProductId));
            Assert.Equal(1, await verify.InventoryMovements.AsNoTracking().CountAsync(x => x.ProductId == fixture.ProductId && x.Type == InventoryMovementType.Sale && x.QuantityChange == -2 && x.PreviousQuantity == 3 && x.NewQuantity == 1));
            Assert.True(await verify.OutboxMessages.AsNoTracking().AnyAsync(x => x.Type == "OrderConfirmation"));
        }
    }
    [Fact]
    public async Task Variant_product_can_checkout_with_test_payment_in_development()
    {
        var fixture = await VariantProduct(stock: 2);
        var token = Guid.NewGuid().ToString("N");
        await using (var db = database.CreateDb())
        {
            var service = Service(db, Environments.Development);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(fixture.ProductId, fixture.VariantId, 1, []), default);
            var checkout = Checkout($"testpay-{Guid.NewGuid():N}");
            checkout.PaymentMethod = PaymentMethod.Test;
            checkout.ShippingMethod = "pickup";

            var result = await service.CheckoutAsync(token, null, checkout, default);

            Assert.Equal(PaymentStatus.Paid, result.PaymentStatus);
            Assert.Equal(1800m, result.Total);
        }
        await using (var verify = database.CreateDb())
        {
            var variant = await verify.ProductVariants.AsNoTracking().SingleAsync(x => x.Id == fixture.VariantId);
            var order = await verify.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Payments).Include(x => x.Shipments).SingleAsync(x => x.Items.Any(i => i.ProductVariantId == fixture.VariantId));

            Assert.Equal(1, variant.StockQuantity);
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
            Assert.Equal(0m, order.ShippingTotal);
            Assert.Equal("pickup", order.Shipments.Single().Service);
            Assert.Equal(PaymentMethod.Test, order.Payments.Single().Method);
            Assert.StartsWith("TEST-", order.Payments.Single().ProviderReference);
            Assert.Equal("Velká", order.Items.Single().VariantName);
            Assert.Equal("VAR-SKU", order.Items.Single().Sku);
        }
    }
    [Fact]
    public async Task Test_payment_is_blocked_outside_development()
    {
        var productId = await Product(1);
        var token = Guid.NewGuid().ToString("N");
        await using (var db = database.CreateDb())
        {
            var service = Service(db, Environments.Production);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
            var checkout = Checkout($"prod-testpay-{Guid.NewGuid():N}");
            checkout.PaymentMethod = PaymentMethod.Test;

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckoutAsync(token, null, checkout, default));
        }
        await using (var verify = database.CreateDb())
        {
            Assert.False(await verify.Orders.AsNoTracking().AnyAsync(x => x.Items.Any(i => i.ProductId == productId)));
            Assert.Equal(1, await verify.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => x.StockQuantity).SingleAsync());
        }
    }
    [Fact]
    public async Task Order_contact_update_saves_addresses_and_validates_phone_without_concurrency_error()
    {
        var productId = await Product(2);
        var token = Guid.NewGuid().ToString("N");
        Guid orderId;
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
            orderId = (await service.CheckoutAsync(token, null, Checkout($"contact-{Guid.NewGuid():N}"), default)).OrderId;
        }
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            var invalid = new OrderContactUpdateRequest
            {
                Email = "buyer@example.test",
                BillingAddress = new AddressInput { FullName = "Ondřej Matoušek", Street = "Kosmická 742", City = "Praha", PostalCode = "14900", CountryCode = "CZ", Phone = "774" },
                ShippingAddress = new AddressInput { FullName = "Ondřej Matoušek", Street = "Kosmická 742", City = "Praha", PostalCode = "14900", CountryCode = "CZ", Phone = "774501987" }
            };

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateOrderContactAsync(orderId, invalid, "admin@example.test", default));

            var valid = new OrderContactUpdateRequest
            {
                Email = "ondrej.matousek@example.test",
                BillingAddress = new AddressInput { FullName = "Ondřej Matoušek", Company = "", Street = "Kosmická 742", City = "Praha", PostalCode = "14900", CountryCode = "CZ", Phone = "774501987" },
                ShippingAddress = new AddressInput { FullName = "Wild Spirit Ateliér", Company = "", Street = "Louka 7", City = "Brno", PostalCode = "60200", CountryCode = "CZ", Phone = "+420 774 501 987" }
            };

            await service.UpdateOrderContactAsync(orderId, valid, "admin@example.test", default);
        }
        await using (var verify = database.CreateDb())
        {
            var order = await verify.Orders.AsNoTracking().Include(x => x.Addresses).Include(x => x.StatusHistory).SingleAsync(x => x.Id == orderId);
            var billing = order.Addresses.Single(x => x.Type == AddressType.Billing);
            var shipping = order.Addresses.Single(x => x.Type == AddressType.Shipping);

            Assert.Equal("ondrej.matousek@example.test", order.Email);
            Assert.Equal("Ondřej Matoušek", billing.FullName);
            Assert.Equal("774501987", billing.Phone);
            Assert.Equal("Wild Spirit Ateliér", shipping.FullName);
            Assert.Equal("+420 774 501 987", shipping.Phone);
            Assert.Contains(order.StatusHistory, x => x.Reason == "Administrace: upraven kontakt a adresa");
        }
    }
    [Fact]
    public async Task User_cart_is_not_visible_to_anonymous_visitor_on_same_browser_after_logout()
    {
        var productId = await Product(3);
        var token = Guid.NewGuid().ToString("N");
        var userId = Guid.NewGuid();
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
            Assert.Single((await service.GetCartAsync(token, userId, "cs-CZ", default)).Items);
        }
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            Assert.Empty((await service.GetCartAsync(token, null, "cs-CZ", default)).Items);
            await Assert.ThrowsAsync<ValidationException>(() => service.CheckoutAsync(token, null, Checkout($"logout-{Guid.NewGuid():N}"), default));
        }
        await using (var db = database.CreateDb())
        {
            Assert.Single((await Service(db).GetCartAsync(token, userId, "cs-CZ", default)).Items);
        }
    }
    [Fact]
    public async Task Cart_culture_is_kept_for_order_snapshot()
    {
        var productId = await Product(2);
        var token = Guid.NewGuid().ToString("N");
        await using (var db = database.CreateDb())
        {
            db.ProductTranslations.Add(new ProductTranslation { ProductId = productId, Culture = "en", Name = "Test product EN", Slug = $"product-en-{Guid.NewGuid():N}" });
            await db.SaveChangesAsync();
        }
        Guid orderId;
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            await service.AddAsync(token, null, "en", new AddToCartRequest(productId, null, 1, []), default);
            orderId = (await service.CheckoutAsync(token, null, Checkout($"culture-{Guid.NewGuid():N}"), default)).OrderId;
        }
        await using var verify = database.CreateDb();
        Assert.Equal("Test product EN", await verify.Set<OrderItem>().Where(x => x.OrderId == orderId).Select(x => x.ProductName).SingleAsync());
    }
    [Fact]
    public async Task Cancelling_paid_gift_voucher_order_deactivates_unused_voucher_codes()
    {
        Guid productId;
        await using (var db = database.CreateDb())
        {
            var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Voucher", Slug = $"voucher-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
            var product = new Product { Category = category, Sku = $"WS-GIFT-VOUCHER-{Guid.NewGuid():N}"[..40], BasePrice = 1000, VatRate = 21, IsPublished = true, AvailabilityStatus = AvailabilityStatus.InStock, StockQuantity = 5, Translations = [new ProductTranslation { Culture = "cs-CZ", Name = "Poukaz", Slug = $"poukaz-{Guid.NewGuid():N}" }] };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            productId = product.Id;
        }
        var token = Guid.NewGuid().ToString("N");
        CheckoutResult result;
        await using (var db = database.CreateDb())
        {
            var service = Service(db, Environments.Development);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
            var checkout = Checkout($"voucher-{Guid.NewGuid():N}");
            checkout.PaymentMethod = PaymentMethod.Test;
            result = await service.CheckoutAsync(token, null, checkout, default);
            Assert.True(await db.DiscountCodes.AsNoTracking().AnyAsync(x => x.CreatedBy == result.OrderNumber && x.IsActive));
            await service.CancelAsync(result.OrderId, "test", default);
        }
        await using var verify = database.CreateDb();
        var codes = await verify.DiscountCodes.AsNoTracking().Where(x => x.CreatedBy == result.OrderNumber).ToListAsync();
        Assert.Single(codes);
        Assert.False(codes[0].IsActive);
    }
    static CommerceService CardService(DesignStore.Infrastructure.Persistence.ApplicationDbContext db, bool enabled = true) => new(db, [new BankTransferPaymentService(), new CardPaymentService(new FakeOnlineGateway(enabled))], new AddToCartRequestValidator(), new CheckoutRequestValidator());
    async Task<CheckoutResult> CardOrder(Guid productId)
    {
        var token = Guid.NewGuid().ToString("N");
        await using var db = database.CreateDb();
        var service = CardService(db);
        await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
        var checkout = Checkout($"card-{Guid.NewGuid():N}");
        checkout.PaymentMethod = PaymentMethod.Card;
        return await service.CheckoutAsync(token, null, checkout, default);
    }
    [Fact]
    public async Task Card_order_is_pending_until_verified_payment_and_payment_is_recorded_once()
    {
        var result = await CardOrder(await Product(2));
        Assert.Equal(PaymentStatus.Pending, result.PaymentStatus);
        var eventId = $"evt_{Guid.NewGuid():N}";
        var paid = new OnlinePaymentEvent(eventId, "checkout.session.completed", true, result.OrderId, "cs_test_paid", (long)(result.Total * 100), "czk");
        await using (var db = database.CreateDb())
        {
            var service = CardService(db);
            Assert.False(await service.ConfirmOnlinePaymentAsync(paid with { EventId = $"evt_{Guid.NewGuid():N}", AmountMinor = 100 }, default));
            Assert.False(await service.ConfirmOnlinePaymentAsync(paid with { EventId = $"evt_{Guid.NewGuid():N}", Currency = "eur" }, default));
            Assert.True(await service.ConfirmOnlinePaymentAsync(paid, default));
        }
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ConfirmOnlinePaymentAsync(paid, default));
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ConfirmOnlinePaymentAsync(paid with { EventId = $"evt_{Guid.NewGuid():N}" }, default));
        var secondSession = paid with { EventId = $"evt_{Guid.NewGuid():N}", SessionId = "cs_test_second" };
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ConfirmOnlinePaymentAsync(secondSession, default));
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ConfirmOnlinePaymentAsync(secondSession, default));

        await using var verify = database.CreateDb();
        var order = await verify.Orders.AsNoTracking().Include(x => x.Payments).Include(x => x.StatusHistory).SingleAsync(x => x.Id == result.OrderId);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(2, order.Payments.Count);
        Assert.Single(order.StatusHistory, x => x.Reason.Contains("zaplacena kartou podruhé"));
        var payment = order.Payments.Single(x => x.ProviderReference == "cs_test_paid");
        Assert.Equal(PaymentMethod.Card, payment.Method);
        Assert.Equal(eventId, payment.ExternalEventId);
        Assert.NotNull(payment.PaidAtUtc);
        Assert.Single(order.StatusHistory, x => x.Reason.StartsWith("Platba kartou přijata"));
    }
    [Fact]
    public async Task Card_payment_for_cancelled_order_is_recorded_but_does_not_reopen_order()
    {
        var result = await CardOrder(await Product(1));
        await using (var db = database.CreateDb()) await CardService(db).CancelAsync(result.OrderId, "test", default);
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ConfirmOnlinePaymentAsync(new OnlinePaymentEvent($"evt_{Guid.NewGuid():N}", "checkout.session.completed", true, result.OrderId, "cs_late", (long)(result.Total * 100), "czk"), default));
        await using var verify = database.CreateDb();
        var order = await verify.Orders.AsNoTracking().Include(x => x.StatusHistory).SingleAsync(x => x.Id == result.OrderId);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Contains(order.StatusHistory, x => x.Reason.Contains("vrátit peníze"));
    }
    [Fact]
    public async Task Starting_card_payment_requires_order_token_and_stores_session_reference()
    {
        var result = await CardOrder(await Product(2));
        await using (var db = database.CreateDb())
        {
            var online = new OnlinePaymentService(db, CardService(db), new FakeOnlineGateway(true), NullLogger<OnlinePaymentService>.Instance);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => online.StartAsync(result.OrderId, "wrong-token", "cs-CZ", "https://shop.test/ok", "https://shop.test/cancel", default));
            Assert.Equal(FakeOnlineGateway.RedirectUrl, await online.StartAsync(result.OrderId, result.AccessToken, "cs-CZ", "https://shop.test/ok", "https://shop.test/cancel", default));
        }
        await using var verify = database.CreateDb();
        Assert.Equal(FakeOnlineGateway.SessionId, await verify.Payments.AsNoTracking().Where(x => x.OrderId == result.OrderId).Select(x => x.ProviderReference).SingleAsync());
    }
    [Fact]
    public async Task Card_checkout_is_refused_when_gateway_is_not_configured_and_stock_is_kept()
    {
        var productId = await Product(1);
        var token = Guid.NewGuid().ToString("N");
        await using (var db = database.CreateDb())
        {
            var service = CardService(db, enabled: false);
            await service.AddAsync(token, null, "cs-CZ", new AddToCartRequest(productId, null, 1, []), default);
            var checkout = Checkout($"card-off-{Guid.NewGuid():N}");
            checkout.PaymentMethod = PaymentMethod.Card;
            await Assert.ThrowsAsync<ValidationException>(() => service.CheckoutAsync(token, null, checkout, default));
        }
        await using var verify = database.CreateDb();
        Assert.Equal(1, await verify.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => x.StockQuantity).SingleAsync());
    }
    [Fact]
    public async Task Expired_latest_payment_page_cancels_unpaid_card_order_and_returns_stock()
    {
        var productId = await Product(1);
        var result = await CardOrder(productId);
        await using (var db = database.CreateDb())
        {
            var gateway = new FakeOnlineGateway(true);
            var online = new OnlinePaymentService(db, CardService(db), gateway, NullLogger<OnlinePaymentService>.Instance);
            await online.StartAsync(result.OrderId, result.AccessToken, "cs-CZ", "https://shop.test/ok", "https://shop.test/cancel", default);
            gateway.WebhookEvent = new OnlinePaymentEvent($"evt_{Guid.NewGuid():N}", "checkout.session.expired", false, result.OrderId, FakeOnlineGateway.SessionId, 0, "czk");
            Assert.Equal(PaymentWebhookResult.Processed, await online.HandleWebhookAsync("{}", "sig", default));
            Assert.Equal(PaymentWebhookResult.Processed, await online.HandleWebhookAsync("{}", "sig", default));
        }
        await using var verify = database.CreateDb();
        var order = await verify.Orders.AsNoTracking().Include(x => x.Payments).Include(x => x.StatusHistory).SingleAsync(x => x.Id == result.OrderId);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Cancelled, Assert.Single(order.Payments).Status);
        Assert.Single(order.StatusHistory, x => x.Reason.Contains("do 24 hodin"));
        Assert.Equal(1, await verify.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => x.StockQuantity).SingleAsync());
    }
    [Fact]
    public async Task Expired_older_payment_page_does_not_cancel_order_with_newer_retry()
    {
        var productId = await Product(1);
        var result = await CardOrder(productId);
        await using (var db = database.CreateDb())
        {
            var online = new OnlinePaymentService(db, CardService(db), new FakeOnlineGateway(true), NullLogger<OnlinePaymentService>.Instance);
            await online.StartAsync(result.OrderId, result.AccessToken, "cs-CZ", "https://shop.test/ok", "https://shop.test/cancel", default);
            Assert.True(await CardService(db).ExpireOnlinePaymentAsync(new OnlinePaymentEvent($"evt_{Guid.NewGuid():N}", "checkout.session.expired", false, result.OrderId, "cs_test_older", 0, "czk"), default));
        }
        await using var verify = database.CreateDb();
        Assert.Equal(OrderStatus.PendingPayment, await verify.Orders.AsNoTracking().Where(x => x.Id == result.OrderId).Select(x => x.Status).SingleAsync());
        Assert.Equal(0, await verify.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => x.StockQuantity).SingleAsync());
    }
    [Fact]
    public async Task Expiry_after_payment_does_not_cancel_paid_order()
    {
        var productId = await Product(1);
        var result = await CardOrder(productId);
        await using (var db = database.CreateDb())
        {
            var online = new OnlinePaymentService(db, CardService(db), new FakeOnlineGateway(true), NullLogger<OnlinePaymentService>.Instance);
            await online.StartAsync(result.OrderId, result.AccessToken, "cs-CZ", "https://shop.test/ok", "https://shop.test/cancel", default);
        }
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ConfirmOnlinePaymentAsync(new OnlinePaymentEvent($"evt_{Guid.NewGuid():N}", "checkout.session.completed", true, result.OrderId, FakeOnlineGateway.SessionId, (long)(result.Total * 100), "czk"), default));
        await using (var db = database.CreateDb()) Assert.True(await CardService(db).ExpireOnlinePaymentAsync(new OnlinePaymentEvent($"evt_{Guid.NewGuid():N}", "checkout.session.expired", false, result.OrderId, FakeOnlineGateway.SessionId, 0, "czk"), default));
        await using var verify = database.CreateDb();
        Assert.Equal(OrderStatus.Paid, await verify.Orders.AsNoTracking().Where(x => x.Id == result.OrderId).Select(x => x.Status).SingleAsync());
        Assert.Equal(0, await verify.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => x.StockQuantity).SingleAsync());
    }
    private sealed class FakeOnlineGateway(bool enabled) : IOnlinePaymentGateway
    {
        public const string SessionId = "cs_test_fake";
        public const string RedirectUrl = "https://checkout.stripe.test/cs_test_fake";
        public bool IsEnabled => enabled;
        public Task<OnlineCheckoutSession> CreateCheckoutSessionAsync(OnlineCheckoutRequest request, CancellationToken ct) => Task.FromResult(new OnlineCheckoutSession(SessionId, RedirectUrl));
        public OnlinePaymentEvent? WebhookEvent { get; set; }
        public bool TryParseWebhook(string payload, string? signatureHeader, out OnlinePaymentEvent? paymentEvent) { paymentEvent = WebhookEvent; return true; }
    }
    async Task<(Guid ProductId, Guid OptionId, Guid OptionValueId)> ConfiguredProduct(int stock)
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Checkout config", Slug = $"checkout-config-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        var optionValue = new ProductOptionValue { PriceAdjustment = 150, Translations = [new ProductOptionValueTranslation { Culture = "cs-CZ", Name = "Mosaz" }] };
        var option = new ProductOption { Type = ProductOptionType.Select, IsRequired = true, Translations = [new ProductOptionTranslation { Culture = "cs-CZ", Name = "Povrch" }], Values = [optionValue] };
        var product = new Product { Category = category, Sku = "CFG-SKU", BasePrice = 1000, VatRate = 21, IsPublished = true, AvailabilityStatus = AvailabilityStatus.InStock, StockQuantity = stock, MainImageUrl = "/media/product/test", Translations = [new ProductTranslation { Culture = "cs-CZ", Name = "Konfigurovaná kabelka", Slug = $"konfigurovana-kabelka-{Guid.NewGuid():N}" }], Options = [option] };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return (product.Id, option.Id, optionValue.Id);
    }
    async Task<(Guid ProductId, Guid VariantId)> VariantProduct(int stock)
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Checkout variant", Slug = $"checkout-variant-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        var variant = new ProductVariant { Sku = "VAR-SKU", Name = "Velká", Price = 1800, AvailabilityStatus = AvailabilityStatus.InStock, StockQuantity = stock, Translations = [new ProductVariantTranslation { Culture = "cs-CZ", Name = "Velká" }] };
        var product = new Product { Category = category, Sku = $"VAR-P-{Guid.NewGuid():N}"[..18], BasePrice = 0, VatRate = 21, IsPublished = true, AvailabilityStatus = AvailabilityStatus.InStock, HasVariants = true, StockQuantity = 0, Translations = [new ProductTranslation { Culture = "cs-CZ", Name = "Variantová kabelka", Slug = $"variantova-kabelka-{Guid.NewGuid():N}" }], Variants = [variant] };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return (product.Id, variant.Id);
    }
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = nameof(CommerceSqlTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
