using DesignStore.Application.Commerce;
using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DesignStore.Infrastructure.Commerce;

/// <summary>Card checkout: the order is created as PendingPayment (stock already reserved) and the customer pays on the gateway's hosted page.</summary>
public sealed class CardPaymentService(IOnlinePaymentGateway gateway) : IPaymentService
{
    public PaymentMethod Method => PaymentMethod.Card;

    public Task<PaymentResult> CreateAsync(string orderNumber, decimal total, string currency, string idempotencyKey, CancellationToken ct)
    {
        if (!gateway.IsEnabled) throw new ValidationException("Platba kartou momentálně není dostupná.");
        return Task.FromResult(new PaymentResult(PaymentStatus.Pending, "", ""));
    }
}

public sealed class OnlinePaymentService(ApplicationDbContext db, ICommerceService commerce, IOnlinePaymentGateway gateway, ILogger<OnlinePaymentService> logger) : IOnlinePaymentService
{
    public bool IsEnabled => gateway.IsEnabled;

    public async Task<string> StartAsync(Guid orderId, string accessToken, string culture, string successUrl, string cancelUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new KeyNotFoundException();
        var tokenHash = CommerceService.Hash(accessToken);
        var order = await db.Orders.Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == orderId && x.AccessTokenHash == tokenHash, ct) ?? throw new KeyNotFoundException();
        if (order.Status != OrderStatus.PendingPayment || order.PaymentStatus == PaymentStatus.Paid) throw new ValidationException("Objednávka již nečeká na platbu.");
        var payment = order.Payments.Where(x => x.Method == PaymentMethod.Card).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()
            ?? throw new ValidationException("Objednávka není placená kartou.");

        var session = await gateway.CreateCheckoutSessionAsync(new OnlineCheckoutRequest(order.Id, order.Number, order.Total, order.Currency, order.Email, culture, successUrl, cancelUrl), ct);
        payment.ProviderReference = session.SessionId;
        payment.Status = PaymentStatus.Pending;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return session.RedirectUrl;
    }

    public async Task<PaymentWebhookResult> HandleWebhookAsync(string payload, string? signatureHeader, CancellationToken ct)
    {
        if (!gateway.TryParseWebhook(payload, signatureHeader, out var paymentEvent)) return PaymentWebhookResult.InvalidSignature;
        if (paymentEvent is null || paymentEvent.OrderId is null || string.IsNullOrEmpty(paymentEvent.EventId)) return PaymentWebhookResult.Ignored;
        if (paymentEvent.Type == "checkout.session.expired")
            return await commerce.ExpireOnlinePaymentAsync(paymentEvent, ct) ? PaymentWebhookResult.Processed : PaymentWebhookResult.Rejected;
        if (!paymentEvent.IsPaid) return PaymentWebhookResult.Ignored;
        if (await commerce.ConfirmOnlinePaymentAsync(paymentEvent, ct)) return PaymentWebhookResult.Processed;
        logger.LogWarning("Payment event {EventId} for order {OrderId} does not match the order (amount {Amount} {Currency}).", paymentEvent.EventId, paymentEvent.OrderId, paymentEvent.AmountMinor, paymentEvent.Currency);
        return PaymentWebhookResult.Rejected;
    }
}
