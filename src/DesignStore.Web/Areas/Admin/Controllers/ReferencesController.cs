using DesignStore.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Policy="CatalogManage")]
public sealed class ReferencesController(ICatalogAdminService catalog):Controller
{
    public async Task<IActionResult> Index(CancellationToken ct){ViewBag.Collections=await catalog.GetCollectionsAsync(ct);ViewBag.Materials=await catalog.GetMaterialsAsync(ct);ViewBag.Colors=await catalog.GetColorsAsync(ct);return View(new ReferenceItemRequest());}
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Save(ReferenceItemRequest model,CancellationToken ct){await catalog.SaveReferenceAsync(model,User.Identity?.Name,ct);return RedirectToAction(nameof(Index));}
}
