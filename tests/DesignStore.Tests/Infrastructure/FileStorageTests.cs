using DesignStore.Application.Abstractions.Files;
using DesignStore.Infrastructure.Files;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
namespace DesignStore.Tests.Infrastructure;

public sealed class FileStorageTests
{
    private static LocalFileStorageService Service() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "FileStorage:Root", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) } }).Build());
    [Fact] public async Task Rejects_disallowed_mime() => await Assert.ThrowsAsync<InvalidDataException>(() => Service().SaveProductImageAsync(new FileUpload(new MemoryStream([1, 2, 3]), "bad.svg", "image/svg+xml", 3), default));
    [Fact] public async Task Rejects_path_traversal() => await Assert.ThrowsAsync<InvalidDataException>(() => Service().OpenReadAsync("../secret.txt", default));
    [Fact]
    public async Task Product_image_upload_is_reencoded_to_webp_with_thumbnail()
    {
        var service = Service();
        await using var input = new MemoryStream();
        using (var image = new Image<Rgba32>(320, 240, new Rgba32(120, 80, 40)))
        {
            await image.SaveAsJpegAsync(input, new JpegEncoder { Quality = 90 });
        }
        input.Position = 0;

        var stored = await service.SaveProductImageAsync(new FileUpload(input, "photo.jpg", "image/jpeg", input.Length), default);

        Assert.Equal("image/webp", stored.ContentType);
        Assert.EndsWith(".webp", stored.StorageKey);
        Assert.EndsWith("-thumb.webp", stored.ThumbnailStorageKey);
        await AssertWebp(service, stored.StorageKey);
        await AssertWebp(service, stored.ThumbnailStorageKey);
    }
    private static async Task AssertWebp(LocalFileStorageService service, string key)
    {
        await using var stream = await service.OpenReadAsync(key, default);
        Assert.NotNull(stream);
        var header = new byte[12];
        Assert.Equal(12, await stream!.ReadAsync(header));
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(header, 0, 4));
        Assert.Equal("WEBP", System.Text.Encoding.ASCII.GetString(header, 8, 4));
    }
}
