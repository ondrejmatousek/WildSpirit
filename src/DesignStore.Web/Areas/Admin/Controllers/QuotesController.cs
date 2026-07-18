using DesignStore.Application.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy="AdminArea")]
public sealed class QuotesController(IInquiryQuoteService service) : Controller
{
    [HttpPost("/{culture:regex(^cs-CZ|en|de$)}/Admin/Quotes/{inquiryId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string culture,Guid inquiryId,decimal deposit,DateTime validUntilUtc,string terms,[FromBody] QuoteLineInput[] items,CancellationToken ct){var quote=await service.CreateQuoteAsync(inquiryId,items.Select(x=>(x.Description,x.Quantity,x.UnitPrice,x.VatRate)).ToArray(),deposit,validUntilUtc,terms,ct);return Ok(new{quote.Id,quote.Number,quote.Version});}
    public sealed record QuoteLineInput(string Description,int Quantity,decimal UnitPrice,decimal VatRate);
}
