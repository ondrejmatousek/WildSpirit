namespace DesignStore.Application.Commerce;

/// <summary>Data for a hosted online payment page (e.g. Stripe Checkout). The amount is the order total snapshot.</summary>
public sealed record OnlineCheckoutRequest(Guid OrderId, string OrderNumber, decimal Amount, string Currency, string CustomerEmail, string Culture, string SuccessUrl, string CancelUrl);

public sealed record OnlineCheckoutSession(string SessionId, string RedirectUrl);

/// <summary>A verified payment event from the gateway. AmountMinor is in the currency's minor unit (haléře, cents).</summary>
public sealed record OnlinePaymentEvent(string EventId, string Type, bool IsPaid, Guid? OrderId, string SessionId, long AmountMinor, string Currency);

public enum PaymentWebhookResult { Processed, Ignored, InvalidSignature, Rejected }

/// <summary>Port to the external card payment provider.</summary>
public interface IOnlinePaymentGateway
{
    bool IsEnabled { get; }
    Task<OnlineCheckoutSession> CreateCheckoutSessionAsync(OnlineCheckoutRequest request, CancellationToken ct);
    /// <summary>Returns false when the signature is missing or invalid. A valid but irrelevant event yields true with a null event.</summary>
    bool TryParseWebhook(string payload, string? signatureHeader, out OnlinePaymentEvent? paymentEvent);
}

public interface IOnlinePaymentService
{
    bool IsEnabled { get; }
    /// <summary>Creates a payment page for a pending card order and returns the URL to redirect the customer to.</summary>
    Task<string> StartAsync(Guid orderId, string accessToken, string culture, string successUrl, string cancelUrl, CancellationToken ct);
    Task<PaymentWebhookResult> HandleWebhookAsync(string payload, string? signatureHeader, CancellationToken ct);
}

/// <summary>The payment provider could not be reached or refused the request. The message is safe to show to customers.</summary>
public sealed class PaymentGatewayException(string message, Exception? inner = null) : Exception(message, inner);
