using DesignStore.Application.Marketing;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Controllers;

public sealed class ContentController(IMarketingService marketing) : Controller
{
    public async Task<IActionResult> Page(string culture, string slug, CancellationToken ct)
    {
        var page = await marketing.GetPageAsync(culture, slug, ct);
        if (page is null) return NotFound();
        ViewData["Title"] = page.SeoTitle ?? page.Title;
        return Content($"<main class='editorial-heading'><p class='eyebrow'>Wild Spirit</p><h1>{page.Title}</h1><p>{page.Summary}</p><article>{page.BodyHtml}</article></main>", "text/html; charset=utf-8");
    }

    public async Task<IActionResult> Gallery(string culture, CancellationToken ct)
    {
        var items = await marketing.GetGalleryAsync(culture, ct);
        var html = string.Join("", items.Select(x => $"<article><img src='{x.ImageUrl}' alt='{x.Title}'/><h2>{x.Title}</h2><p>{x.Description}</p></article>"));
        return Content($"<main class='editorial-heading'><p class='eyebrow'>Realizace</p><h1>Galerie</h1>{html}</main>", "text/html; charset=utf-8");
    }
}
