using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Controllers;

public sealed class MediaController(IFileStorageService storage, ICatalogAdminService catalog) : Controller
{
    [HttpGet("media/product/{id:guid}")] public async Task<IActionResult> Product(Guid id, bool thumbnail, CancellationToken ct) { var image = await catalog.GetProductImageAsync(id, thumbnail, ct); if (image is null) return NotFound(); var stream = await storage.OpenReadAsync(image.StorageKey, ct); return stream is null ? NotFound() : File(stream, image.ContentType, enableRangeProcessing: true); }
    [HttpGet("media/category-image/{*storageKey}")] public async Task<IActionResult> CategoryImage(string storageKey, CancellationToken ct) { if (string.IsNullOrWhiteSpace(storageKey) || !storageKey.StartsWith("categories/", StringComparison.OrdinalIgnoreCase)) return NotFound(); var stream = await storage.OpenReadAsync(storageKey, ct); return stream is null ? NotFound() : File(stream, "image/webp", enableRangeProcessing: true); }
}
