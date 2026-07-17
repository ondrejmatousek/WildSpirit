namespace DesignStore.Application.Abstractions.Files;
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
public sealed record StoredImage(string StorageKey, string ThumbnailStorageKey, string ContentType, int Width, int Height);
public interface IFileStorageService
{
    Task<StoredImage> SaveProductImageAsync(FileUpload upload, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
