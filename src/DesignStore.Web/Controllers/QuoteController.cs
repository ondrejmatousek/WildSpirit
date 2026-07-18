using DesignStore.Application.Commerce;
using DesignStore.Domain.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Controllers;

public sealed class QuoteController(IInquiryQuoteService service) : Controller
{
    [HttpGet("/{culture:regex(^cs-CZ|en|de$)}/quote/{token}")]
    public async Task<IActionResult> Show(string culture,string token,CancellationToken ct){var quote=await service.GetQuoteAsync(token,ct);if(quote is null)return NotFound();return Content($"<h1>{quote.Number}</h1><p>{quote.Total:N2} {quote.Currency}</p><form method='post'><input type='hidden' name='response' value='Accepted'/><button>Přijmout nabídku</button></form>","text/html");}
    [HttpPost("/{culture:regex(^cs-CZ|en|de$)}/quote/{token}/respond")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(string culture,string token,QuoteStatus response,string? comment,CancellationToken ct){try{await service.RespondToQuoteAsync(token,response,comment,ct);return RedirectToAction(nameof(Show),new{culture,token});}catch(Exception ex) when(ex is FluentValidation.ValidationException or KeyNotFoundException){return BadRequest(ex.Message);}}
}
