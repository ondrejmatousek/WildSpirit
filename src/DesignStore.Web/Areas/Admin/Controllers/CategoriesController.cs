using DesignStore.Application.Catalog;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Policy="CatalogManage")]
public sealed class CategoriesController(ICatalogAdminService catalog):Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)=>View(await catalog.GetCategoriesAsync(ct));
    public async Task<IActionResult> Edit(Guid? id,CancellationToken ct){var m=id.HasValue?await catalog.GetCategoryAsync(id.Value,ct):new EditCategoryRequest();if(m is null)return NotFound();Ensure(m);ViewBag.Categories=await catalog.GetCategoriesAsync(ct);return View(m);}
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Save(EditCategoryRequest m,CancellationToken ct){try{await catalog.SaveCategoryAsync(m,User.Identity?.Name,ct);return RedirectToAction(nameof(Index));}catch(ValidationException ex){ModelState.AddModelError(string.Empty,ex.Message);}Ensure(m);ViewBag.Categories=await catalog.GetCategoriesAsync(ct);return View("Edit",m);}
    private static void Ensure(EditCategoryRequest m){foreach(var c in new[]{"cs-CZ","en","de"})if(m.Translations.All(x=>x.Culture!=c))m.Translations.Add(new CategoryTranslationInput{Culture=c});}
}
