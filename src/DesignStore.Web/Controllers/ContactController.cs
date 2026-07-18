using DesignStore.Application.Marketing;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Controllers;

public sealed class ContactController(IMarketingService marketing) : Controller
{
    public IActionResult Index() => View(new ContactRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string culture, ContactRequest request, CancellationToken ct)
    {
        request.Culture = culture;
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await marketing.SendContactAsync(request, ct);
        TempData["ContactSent"] = "Děkujeme, zpráva byla odeslána.";
        return RedirectToAction(nameof(Index), new { culture });
    }
}
