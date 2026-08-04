using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Catalog;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "CatalogManage")]
public sealed class CategoriesController(ICatalogAdminService catalog, IFileStorageService storage) : Controller
{
    private const long MaximumSingleImageBytes = 12 * 1024 * 1024;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await catalog.GetCategoriesAsync(ct));

    public async Task<IActionResult> Edit(Guid? id, CancellationToken ct)
    {
        var model = id.HasValue ? await catalog.GetCategoryAsync(id.Value, ct) : new EditCategoryRequest();
        if (model is null) return NotFound();

        Ensure(model);
        ViewBag.Categories = await catalog.GetCategoriesAsync(ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2 * MaximumSingleImageBytes)]
    public async Task<IActionResult> Save(EditCategoryRequest model, IFormFile? imageFile, IFormFile? bannerFile, CancellationToken ct)
    {
        try
        {
            var id = await catalog.SaveCategoryAsync(model, User.Identity?.Name, ct);
            model.Id = id;

            if (imageFile is { Length: > 0 })
            {
                model.ImageUrl = await SaveCategoryImageAsync(imageFile, ct);
            }

            if (bannerFile is { Length: > 0 })
            {
                model.BannerUrl = await SaveCategoryImageAsync(bannerFile, ct);
            }

            if (imageFile is { Length: > 0 } || bannerFile is { Length: > 0 })
            {
                await catalog.SaveCategoryAsync(model, User.Identity?.Name, ct);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        Ensure(model);
        ViewBag.Categories = await catalog.GetCategoriesAsync(ct);
        return View("Edit", model);
    }

    private async Task<string> SaveCategoryImageAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var stored = await storage.SaveImageAsync(new FileUpload(stream, file.FileName, file.ContentType, file.Length), "categories", ct);
        return $"/media/category-image/{stored.StorageKey}";
    }

    private static void Ensure(EditCategoryRequest model)
    {
        foreach (var culture in new[] { "cs-CZ", "en", "de" })
        {
            if (model.Translations.All(x => x.Culture != culture))
            {
                model.Translations.Add(new CategoryTranslationInput { Culture = culture });
            }
        }

        model.Translations = model.Translations
            .OrderBy(x => x.Culture == "cs-CZ" ? 0 : x.Culture == "en" ? 1 : 2)
            .ToList();
    }
}
