using DesignStore.Application.Marketing;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Controllers;

public sealed class NewsletterController(IMarketingService marketing) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(string culture, NewsletterSubscribeRequest request, CancellationToken ct)
    {
        request.Culture = culture;
        var result = await marketing.SubscribeAsync(request, ct);
        return Ok(result.AlreadyConfirmed ? "E-mail je už potvrzený." : "Poslali jsme potvrzovací e-mail.");
    }

    public async Task<IActionResult> Confirm(string culture, string token, CancellationToken ct)
        => Content(await marketing.ConfirmNewsletterAsync(token, ct) ? "Newsletter potvrzen." : "Neplatný odkaz.");

    public async Task<IActionResult> Unsubscribe(string culture, string token, CancellationToken ct)
        => Content(await marketing.UnsubscribeNewsletterAsync(token, ct) ? "Odhlášení potvrzeno." : "Neplatný odkaz.");
}
