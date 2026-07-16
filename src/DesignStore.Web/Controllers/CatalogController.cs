using DesignStore.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Controllers;
public sealed class CatalogController(ICatalogService catalog) : Controller
{
    public async Task<IActionResult> Index(string culture = "cs-CZ", Guid? categoryId = null, CancellationToken ct = default) { ViewBag.Categories = await catalog.GetCategoriesAsync(culture, ct); return View(await catalog.GetProductsAsync(culture, categoryId, ct)); }
    [HttpGet("{culture:regex(^cs-CZ|en|de$)}/product/{slug}")]
    public async Task<IActionResult> Detail(string culture, string slug, CancellationToken ct) { var product = await catalog.GetProductAsync(culture, slug, ct); return product is null ? NotFound() : View(product); }
}
