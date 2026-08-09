using DesignStore.Application.Catalog;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "CatalogManage")]
public sealed class ProductsController(ICatalogAdminService catalog) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await catalog.GetProductsAsync(ct));
    public async Task<IActionResult> Create(CancellationToken ct) { var model = new EditProductRequest { Sku = await catalog.GetNextProductSkuAsync(ct), Translations = [Translation("cs-CZ"), Translation("en"), Translation("de")] }; await Lookups(ct); return View("Edit", model); }
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct) { var model = await catalog.GetProductAsync(id, ct); if (model is null) return NotFound(); EnsureCultures(model); await Lookups(ct); return View(model); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EditProductRequest model, CancellationToken ct)
    {
        try { var id = await catalog.SaveProductAsync(model, User.Identity?.Name, ct); TempData["Success"] = "Produkt byl uložen."; return RedirectToAction(nameof(Edit), new { id }); }
        catch (ValidationException ex) { foreach (var e in ex.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage); if (!ex.Errors.Any()) ModelState.AddModelError(string.Empty, ex.Message); }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "Produkt nelze uložit kvůli konfliktu unikátních údajů."); }
        EnsureCultures(model); await Lookups(ct); return View("Edit", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) { await catalog.ArchiveProductAsync(id, User.Identity?.Name, ct); return RedirectToAction(nameof(Index)); }
    private async Task Lookups(CancellationToken ct) { ViewBag.Categories = await catalog.GetCategoriesAsync(ct); ViewBag.Collections = await catalog.GetCollectionsAsync(ct); ViewBag.Materials = await catalog.GetMaterialsAsync(ct); }
    private static ProductTranslationInput Translation(string culture) => new() { Culture = culture };
    private static void EnsureCultures(EditProductRequest m) { foreach (var c in new[] { "cs-CZ", "en", "de" }) if (m.Translations.All(x => x.Culture != c)) m.Translations.Add(Translation(c)); }
}
