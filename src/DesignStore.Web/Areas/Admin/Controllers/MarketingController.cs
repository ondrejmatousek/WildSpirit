using DesignStore.Application.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "AdminArea")]
public sealed class MarketingController(IMarketingService marketing) : Controller
{
    public IActionResult Index() => Content("Marketing: newsletter export, obsah, galerie a recenze.");

    public async Task<IActionResult> NewsletterCsv(CancellationToken ct)
        => File(System.Text.Encoding.UTF8.GetBytes(await marketing.ExportNewsletterCsvAsync(ct)), "text/csv", "newsletter.csv");
}
