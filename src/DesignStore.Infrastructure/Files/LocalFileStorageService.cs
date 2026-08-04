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

    public LocalFileStorageService(IConfiguration configuration)
    {
        _root = Path.GetFullPath(configuration["FileStorage:Root"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "files"));
        Directory.CreateDirectory(_root);
    }

    public Task<StoredImage> SaveProductImageAsync(FileUpload upload, CancellationToken ct) => SaveImageAsync(upload, "products", ct);

    public async Task<StoredImage> SaveImageAsync(FileUpload upload, string folder, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(folder) ||
            folder.Contains("..", StringComparison.Ordinal) ||
            folder.Any(ch => !char.IsLetterOrDigit(ch) && ch is not '-' and not '_'))
        {
            throw new InvalidDataException("Neplatná složka obrázku.");
        }

        if (upload.Length is <= 0 or > MaximumImageBytes)
        {
            throw new InvalidDataException("Obrázek má neplatnou velikost.");
        }

        if (upload.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            throw new InvalidDataException("Nepovolený MIME typ obrázku.");
        }

        await using var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, ct);
        if (buffer.Length != upload.Length || buffer.Length > MaximumImageBytes)
        {
            throw new InvalidDataException("Velikost obsahu neodpovídá deklarované velikosti.");
        }

        buffer.Position = 0;
        using var image = await Image.LoadAsync(buffer, ct);
        if (image.Width < 120 || image.Height < 120 || (long)image.Width * image.Height > 60_000_000)
        {
            throw new InvalidDataException("Rozměry obrázku nejsou povolené.");
        }

        image.Metadata.ExifProfile = null;
        image.Mutate(x => x.AutoOrient());

        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var name = Guid.NewGuid().ToString("N");
        var original = $"{folder}/{dateFolder}/{name}.webp";
        var thumb = $"{folder}/{dateFolder}/{name}-thumb.webp";

        await SaveWebpAsync(image, original, 2200, 84, ct);
        await SaveWebpAsync(image, thumb, 640, 78, ct);

        return new StoredImage(original, thumb, "image/webp", image.Width, image.Height);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct)
    {
        var path = Resolve(key);
        return Task.FromResult<Stream?>(File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true) : null);
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<StoredAttachment> SaveAttachmentAsync(FileUpload upload, CancellationToken ct)
    {
        if (upload.Length <= 0 || upload.Length > 10 * 1024 * 1024)
        {
            throw new InvalidDataException("Příloha má nepovolenou velikost.");
        }

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Typ přílohy není povolen.");
        }

        await using var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, ct);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray()));
        var key = $"inquiries/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.bin";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        buffer.Position = 0;
        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
        {
            await buffer.CopyToAsync(file, ct);
        }

        return new StoredAttachment(key, Path.GetFileName(upload.FileName), upload.ContentType, upload.Length, hash);
    }

    private async Task SaveWebpAsync(Image source, string key, int maxWidth, int quality, CancellationToken ct)
    {
        using var image = source.Clone(x =>
        {
            if (source.Width > maxWidth)
            {
                x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxWidth, maxWidth) });
            }
        });

        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await image.SaveAsWebpAsync(path, new WebpEncoder { Quality = quality }, ct);
    }

    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Neplatný klíč souboru.");
        }

        var path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Cesta opouští úložiště.");
        }

        return path;
    }
}
