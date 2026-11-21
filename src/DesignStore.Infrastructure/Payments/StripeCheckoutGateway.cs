using DesignStore.Application.Commerce;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DesignStore.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";
    /// <summary>Secret API key (sk_test_… / sk_live_…). Keep it in user secrets or environment variables, never in the repository.</summary>
    public string SecretKey { get; set; } = "";
    /// <summary>Webhook signing secret (whsec_…) of the endpoint /payments/stripe/webhook.</summary>
    public string WebhookSecret { get; set; } = "";
}

/// <summary>
/// Stripe Checkout (hosted payment page) over the plain REST API: card data never touches this application.
/// </summary>
public sealed class StripeCheckoutGateway(HttpClient http, IOptions<StripeOptions> options, TimeProvider time, ILogger<StripeCheckoutGateway> logger) : IOnlinePaymentGateway
{
    public const string ApiBaseAddress = "https://api.stripe.com/";
    const long SignatureToleranceSeconds = 300;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(options.Value.SecretKey) && !string.IsNullOrWhiteSpace(options.Value.WebhookSecret);

    public async Task<OnlineCheckoutSession> CreateCheckoutSessionAsync(OnlineCheckoutRequest r, CancellationToken ct)
    {
        if (!IsEnabled) throw new PaymentGatewayException("Platba kartou momentálně není dostupná.");
        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("client_reference_id", r.OrderId.ToString()),
            new("success_url", r.SuccessUrl),
            new("cancel_url", r.CancelUrl),
            new("customer_email", r.CustomerEmail),
            new("locale", Locale(r.Culture)),
            new("line_items[0][quantity]", "1"),
            new("line_items[0][price_data][currency]", r.Currency.ToLowerInvariant()),
            new("line_items[0][price_data][unit_amount]", ToMinorUnits(r.Amount).ToString(CultureInfo.InvariantCulture)),
            new("line_items[0][price_data][product_data][name]", $"Wild Spirit {r.OrderNumber}"),
            new("metadata[order_id]", r.OrderId.ToString()),
            new("metadata[order_number]", r.OrderNumber),
            new("payment_intent_data[metadata][order_number]", r.OrderNumber)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/checkout/sessions") { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.SecretKey);
        try
        {
            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Stripe checkout session for {OrderNumber} failed with {StatusCode}: {StripeError}", r.OrderNumber, (int)response.StatusCode, StripeErrorMessage(body));
                throw new PaymentGatewayException("Platební bránu se nepodařilo spustit. Zkuste to prosím znovu.");
            }

            using var json = JsonDocument.Parse(body);
            var id = json.RootElement.GetProperty("id").GetString();
            var url = json.RootElement.GetProperty("url").GetString();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(url)) throw new PaymentGatewayException("Platební brána vrátila neúplnou odpověď.");
            return new OnlineCheckoutSession(id, url);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogError(e, "Stripe checkout session for {OrderNumber} could not be created.", r.OrderNumber);
            throw new PaymentGatewayException("Platební bránu se nepodařilo kontaktovat. Zkuste to prosím znovu.", e);
        }
    }

    public bool TryParseWebhook(string payload, string? signatureHeader, out OnlinePaymentEvent? paymentEvent)
    {
        paymentEvent = null;
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signatureHeader)) return false;

        long? timestamp = null;
        var signatures = new List<string>();
        foreach (var part in signatureHeader.Split(','))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2) continue;
            var key = pair[0].Trim();
            if (key == "t" && long.TryParse(pair[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var t)) timestamp = t;
            else if (key == "v1") signatures.Add(pair[1].Trim());
        }
        if (timestamp is null || signatures.Count == 0) return false;
        if (Math.Abs(time.GetUtcNow().ToUnixTimeSeconds() - timestamp.Value) > SignatureToleranceSeconds) return false;

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp.Value.ToString(CultureInfo.InvariantCulture)}.{payload}"));
        if (!signatures.Any(s => IsHex(s) && CryptographicOperations.FixedTimeEquals(Convert.FromHexString(s), expected))) return false;

        try
        {
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            var type = root.GetProperty("type").GetString() ?? "";
            if (!type.StartsWith("checkout.session.", StringComparison.Ordinal)) return true;
            var session = root.GetProperty("data").GetProperty("object");
            var paymentStatus = Text(session, "payment_status");
            var isPaid = (type == "checkout.session.completed" && paymentStatus == "paid") || type == "checkout.session.async_payment_succeeded";
            Guid? orderId = Guid.TryParse(Text(session, "client_reference_id"), out var id) ? id : null;
            var amount = session.TryGetProperty("amount_total", out var a) && a.ValueKind == JsonValueKind.Number ? a.GetInt64() : 0;
            paymentEvent = new OnlinePaymentEvent(root.GetProperty("id").GetString() ?? "", type, isPaid, orderId, Text(session, "id"), amount, Text(session, "currency"));
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            logger.LogWarning(e, "Stripe webhook with a valid signature could not be parsed.");
        }
        return true;
    }

    /// <summary>Stripe expects amounts in the currency's minor unit (CZK and EUR use 2 decimals).</summary>
    public static long ToMinorUnits(decimal amount)
    {
        var minor = amount * 100m;
        if (minor != decimal.Truncate(minor)) throw new PaymentGatewayException("Částku objednávky nelze předat platební bráně.");
        return (long)minor;
    }

    static string Locale(string culture) => culture switch { "cs-CZ" => "cs", "de" => "de", "en" => "en", _ => "auto" };
    static string Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    static bool IsHex(string s) => s.Length % 2 == 0 && s.All(Uri.IsHexDigit);
    static string StripeErrorMessage(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("error", out var error) ? $"{Text(error, "type")} {Text(error, "code")} {Text(error, "message")}".Trim() : "unknown";
        }
        catch (JsonException) { return "unreadable response"; }
    }
}
