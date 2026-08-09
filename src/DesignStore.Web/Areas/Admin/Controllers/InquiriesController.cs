using DesignStore.Application.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "AdminArea")]
public sealed class InquiriesController(IInquiryQuoteService service) : Controller
{
    [HttpGet("/{culture:regex(^cs-CZ|en|de$)}/Admin/Inquiries")]
    public async Task<IActionResult> Index(string culture, CancellationToken ct) => Content(string.Join("\n", (await service.GetInquiriesAsync(ct)).Select(x => $"{x.Number} | {x.Status} | {x.ProductName} | {x.Email}")), "text/plain");
}
