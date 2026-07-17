using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Policy="CatalogManage")]
public sealed class MediaController(IFileStorageService storage,ICatalogAdminService catalog):Controller
{
    [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(12*1024*1024)]
    public async Task<IActionResult> Upload(Guid productId,IFormFile file,string altText,bool isPrimary,CancellationToken ct)
    {
        if(file.Length==0){TempData["Error"]="Vyberte obrázek.";return RedirectToAction("Edit","Products",new{id=productId});}
        try{await using var stream=file.OpenReadStream();var stored=await storage.SaveProductImageAsync(new FileUpload(stream,file.FileName,file.ContentType,file.Length),ct);await catalog.AddProductImageAsync(productId,stored,altText,isPrimary,User.Identity?.Name,ct);TempData["Success"]="Obrázek byl uložen.";}catch(InvalidDataException ex){TempData["Error"]=ex.Message;}return RedirectToAction("Edit","Products",new{id=productId});
    }
}
