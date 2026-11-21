using DesignStore.Application.Commerce;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DesignStore.Web.Controllers;

/// <summary>Stripe calls this endpoint server-to-server; authenticity is proven by the Stripe-Signature header, not by cookies.</summary>
[Route("payments/stripe")]
public sealed class PaymentWebhookController(IOnlinePaymentService onlinePayments, ILogger<PaymentWebhookController> logger) : Controller
{
    [HttpPost("webhook"), IgnoreAntiforgeryToken, RequestSizeLimit(256 * 1024)]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);
        var result = await onlinePayments.HandleWebhookAsync(payload, Request.Headers["Stripe-Signature"].ToString(), ct);
        if (result == PaymentWebhookResult.InvalidSignature)
        {
            logger.LogWarning("Rejected Stripe webhook with an invalid or missing signature.");
            return BadRequest();
        }

        // Processed, ignored and rejected (logged by the service) events are acknowledged so Stripe stops retrying.
        return Ok();
    }
}
