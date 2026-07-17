using DesignStore.Application.Abstractions.Files;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
namespace DesignStore.Infrastructure.Files;
public sealed class LocalFileStorageService : IFileStorageService
{
    public const long MaximumImageBytes = 12 * 1024 * 1024;
    private readonly string _root;
    public LocalFileStorageService(IConfiguration configuration) { _root=Path.GetFullPath(configuration["FileStorage:Root"]??Path.Combine(AppContext.BaseDirectory,"App_Data","files"));Directory.CreateDirectory(_root); }
    public async Task<StoredImage> SaveProductImageAsync(FileUpload upload,CancellationToken ct)
    {
        if(upload.Length is <=0 or >MaximumImageBytes)throw new InvalidDataException("Obrázek má neplatnou velikost.");
        if(upload.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))throw new InvalidDataException("Nepovolený MIME typ obrázku.");
        await using var buffer=new MemoryStream();await upload.Content.CopyToAsync(buffer,ct);if(buffer.Length!=upload.Length||buffer.Length>MaximumImageBytes)throw new InvalidDataException("Velikost obsahu neodpovídá deklarované velikosti.");buffer.Position=0;
        using var image=await Image.LoadAsync(buffer,ct);if(image.Width<120||image.Height<120||(long)image.Width*image.Height>60_000_000)throw new InvalidDataException("Rozměry obrázku nejsou povolené.");image.Metadata.ExifProfile=null;image.Mutate(x=>x.AutoOrient());
        var folder=DateTime.UtcNow.ToString("yyyy/MM");var name=Guid.NewGuid().ToString("N");var original=$"products/{folder}/{name}.webp";var thumb=$"products/{folder}/{name}-thumb.webp";await SaveWebpAsync(image,original,2200,84,ct);await SaveWebpAsync(image,thumb,640,78,ct);return new StoredImage(original,thumb,"image/webp",image.Width,image.Height);
    }
    public Task<Stream?> OpenReadAsync(string key,CancellationToken ct){var path=Resolve(key);return Task.FromResult<Stream?>(File.Exists(path)?new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,true):null);}
    public Task DeleteAsync(string key,CancellationToken ct){var path=Resolve(key);if(File.Exists(path))File.Delete(path);return Task.CompletedTask;}
    private async Task SaveWebpAsync(Image source,string key,int maxWidth,int quality,CancellationToken ct){using var image=source.Clone(x=>{if(source.Width>maxWidth)x.Resize(new ResizeOptions{Mode=ResizeMode.Max,Size=new Size(maxWidth,maxWidth)});});var path=Resolve(key);Directory.CreateDirectory(Path.GetDirectoryName(path)!);await image.SaveAsWebpAsync(path,new WebpEncoder{Quality=quality},ct);}
    private string Resolve(string key){if(string.IsNullOrWhiteSpace(key)||Path.IsPathRooted(key)||key.Contains("..",StringComparison.Ordinal))throw new InvalidDataException("Neplatný klíč souboru.");var path=Path.GetFullPath(Path.Combine(_root,key.Replace('/',Path.DirectorySeparatorChar)));if(!path.StartsWith(_root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Cesta opouští úložiště.");return path;}
}
