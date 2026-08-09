using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Controllers;

public sealed class InquiryController(IInquiryQuoteService service) : Controller
{
    [HttpGet("/{culture:regex(^cs-CZ|en|de$)}/inquiry/{productId:guid}")]
    public IActionResult Create(string culture, Guid productId, Guid? variantId) => Content($"<h1>Poptávka výroby</h1><p>Produkt: {productId}</p><form method='post'><input name='FullName' required placeholder='Jméno'/><input name='Email' type='email' required placeholder='E-mail'/><input name='CountryCode' value='CZ'/><input name='Quantity' type='number' min='1' value='1'/><input type='hidden' name='ProductId' value='{productId}'/><input type='hidden' name='ProductVariantId' value='{variantId}'/><label><input type='checkbox' name='TermsAccepted' value='true' required/> Souhlasím</label><button>Odeslat</button></form>", "text/html");
    [HttpPost("/{culture:regex(^cs-CZ|en|de$)}/inquiry/{productId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string culture, Guid productId, CreateInquiryRequest request, CancellationToken ct) { if (request.ProductId != productId) request = request with { ProductId = productId }; try { var inquiry = await service.CreateInquiryAsync(request, ct); return RedirectToAction(nameof(Sent), new { culture, number = inquiry.Number }); } catch (Exception ex) when (ex is FluentValidation.ValidationException or KeyNotFoundException) { return BadRequest(ex.Message); } }
    [HttpGet("/{culture:regex(^cs-CZ|en|de$)}/inquiry/sent")]
    public IActionResult Sent(string culture, string number) => Content($"<h1>Poptávka {number} byla odeslána.</h1>", "text/html");
    [HttpPost("/{culture:regex(^cs-CZ|en|de$)}/inquiry/{inquiryId:guid}/attachment"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Attachment(string culture, Guid inquiryId, IFormFile file, CancellationToken ct) { if (file is null) return BadRequest(); try { await using var stream = file.OpenReadStream(); await service.AddAttachmentAsync(inquiryId, new FileUpload(stream, file.FileName, file.ContentType, file.Length), ct); return Ok(); } catch (InvalidDataException ex) { return BadRequest(ex.Message); } }
}
