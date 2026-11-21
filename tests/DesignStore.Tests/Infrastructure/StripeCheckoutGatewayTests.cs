using DesignStore.Application.Commerce;
using DesignStore.Infrastructure.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DesignStore.Tests.Infrastructure;

public sealed class StripeCheckoutGatewayTests
{
    const string WebhookSecret = "whsec_unit_test_secret";
    static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);
    static readonly Guid OrderId = Guid.Parse("8a3c0f1e-2b4d-4c6e-9f10-112233445566");

    [Fact]
    public void Valid_signature_parses_paid_checkout_session()
    {
        var payload = SessionCompletedPayload("paid", 146000);
        Assert.True(Gateway().TryParseWebhook(payload, Sign(payload, Now), out var e));
        Assert.NotNull(e);
        Assert.True(e.IsPaid);
        Assert.Equal(OrderId, e.OrderId);
        Assert.Equal("evt_1", e.EventId);
        Assert.Equal("cs_test_1", e.SessionId);
        Assert.Equal(146000, e.AmountMinor);
        Assert.Equal("czk", e.Currency);
    }

    [Fact]
    public void Unpaid_completed_session_is_not_treated_as_paid()
    {
        var payload = SessionCompletedPayload("unpaid", 146000);
        Assert.True(Gateway().TryParseWebhook(payload, Sign(payload, Now), out var e));
        Assert.False(e!.IsPaid);
    }

    [Fact]
    public void Tampered_payload_wrong_secret_or_old_timestamp_is_rejected()
    {
        var payload = SessionCompletedPayload("paid", 146000);
        var signature = Sign(payload, Now);
        Assert.False(Gateway().TryParseWebhook(payload.Replace("146000", "100"), signature, out _));
        Assert.False(Gateway().TryParseWebhook(payload, Sign(payload, Now, "whsec_other"), out _));
        Assert.False(Gateway().TryParseWebhook(payload, Sign(payload, Now.AddMinutes(-10)), out _));
        Assert.False(Gateway().TryParseWebhook(payload, null, out _));
        Assert.False(Gateway().TryParseWebhook(payload, "t=abc,v1=zz", out _));
    }

    [Fact]
    public void Unrelated_event_with_valid_signature_is_ignored()
    {
        var payload = """{"id":"evt_2","type":"customer.created","data":{"object":{"id":"cus_1"}}}""";
        Assert.True(Gateway().TryParseWebhook(payload, Sign(payload, Now), out var e));
        Assert.Null(e);
    }

    [Fact]
    public async Task Checkout_session_request_sends_order_total_in_minor_units()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"id":"cs_test_1","url":"https://checkout.stripe.com/c/pay/cs_test_1"}""");
        var session = await Gateway(handler).CreateCheckoutSessionAsync(new OnlineCheckoutRequest(OrderId, "WS-20261121-001", 1460.00m, "CZK", "buyer@example.test", "de", "https://shop.test/ok", "https://shop.test/cancel"), default);

        Assert.Equal("cs_test_1", session.SessionId);
        Assert.Equal("https://checkout.stripe.com/c/pay/cs_test_1", session.RedirectUrl);
        Assert.Equal("https://api.stripe.com/v1/checkout/sessions", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer sk_test_unit", handler.Request.Headers.Authorization!.ToString());
        var form = handler.Body!.Split('&').Select(WebUtility.UrlDecode).ToList();
        Assert.Contains("mode=payment", form);
        Assert.Contains($"client_reference_id={OrderId}", form);
        Assert.Contains("line_items[0][price_data][unit_amount]=146000", form);
        Assert.Contains("line_items[0][price_data][currency]=czk", form);
        Assert.Contains("locale=de", form);
    }

    [Fact]
    public async Task Stripe_error_becomes_safe_gateway_exception()
    {
        var handler = new RecordingHandler(HttpStatusCode.BadRequest, """{"error":{"type":"invalid_request_error","message":"secret detail"}}""");
        var e = await Assert.ThrowsAsync<PaymentGatewayException>(() => Gateway(handler).CreateCheckoutSessionAsync(new OnlineCheckoutRequest(OrderId, "WS-1", 100m, "CZK", "buyer@example.test", "cs-CZ", "https://shop.test/ok", "https://shop.test/cancel"), default));
        Assert.DoesNotContain("secret detail", e.Message);
    }

    [Fact]
    public void Gateway_is_disabled_without_keys()
    {
        var gateway = new StripeCheckoutGateway(new HttpClient(), Options.Create(new StripeOptions()), new FixedTime(Now), NullLogger<StripeCheckoutGateway>.Instance);
        Assert.False(gateway.IsEnabled);
        Assert.False(gateway.TryParseWebhook("{}", "t=1,v1=00", out _));
    }

    static StripeCheckoutGateway Gateway(HttpMessageHandler? handler = null) =>
        new(new HttpClient(handler ?? new RecordingHandler(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri(StripeCheckoutGateway.ApiBaseAddress) },
            Options.Create(new StripeOptions { SecretKey = "sk_test_unit", WebhookSecret = WebhookSecret }), new FixedTime(Now), NullLogger<StripeCheckoutGateway>.Instance);

    static string SessionCompletedPayload(string paymentStatus, long amount) =>
        "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_test_1\",\"payment_status\":\"" + paymentStatus + "\",\"amount_total\":" + amount + ",\"currency\":\"czk\",\"client_reference_id\":\"" + OrderId + "\"}}}";

    internal static string Sign(string payload, DateTimeOffset at, string secret = WebhookSecret)
    {
        var timestamp = at.ToUnixTimeSeconds();
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    sealed class FixedTime(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }

    sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
