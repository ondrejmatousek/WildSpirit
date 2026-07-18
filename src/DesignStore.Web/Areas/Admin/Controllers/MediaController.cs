using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "CatalogManage")]
public sealed class MediaController(IFileStorageService storage, ICatalogAdminService catalog) : Controller
{
    private const int MaximumFilesPerUpload = 5;
    private const long MaximumSingleImageBytes = 12 * 1024 * 1024;

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaximumFilesPerUpload * MaximumSingleImageBytes)]
    public async Task<IActionResult> Upload(Guid productId, List<IFormFile> files, string altText, bool isPrimary, CancellationToken ct)
    {
        if (files.Count == 0 || files.All(file => file.Length == 0))
        {
            TempData["Error"] = "Vyberte alespoň jeden obrázek.";
            return RedirectToAction("Edit", "Products", new { id = productId });
        }

        if (files.Count > MaximumFilesPerUpload)
        {
            TempData["Error"] = $"Najednou lze nahrát maximálně {MaximumFilesPerUpload} obrázků.";
            return RedirectToAction("Edit", "Products", new { id = productId });
        }

        var uploaded = 0;
        try
        {
            foreach (var file in files.Where(file => file.Length > 0))
            {
                await using var stream = file.OpenReadStream();
                var stored = await storage.SaveProductImageAsync(new FileUpload(stream, file.FileName, file.ContentType, file.Length), ct);
                await catalog.AddProductImageAsync(productId, stored, altText, isPrimary && uploaded == 0, User.Identity?.Name, ct);
                uploaded++;
            }
            TempData["Success"] = uploaded == 1 ? "Obrázek byl uložen." : $"Obrázky byly uloženy ({uploaded}).";
        }
        catch (InvalidDataException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Edit", "Products", new { id = productId });
    }
}
