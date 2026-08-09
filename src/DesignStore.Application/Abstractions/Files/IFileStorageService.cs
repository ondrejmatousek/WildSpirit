namespace DesignStore.Application.Abstractions.Files;

public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
public sealed record StoredImage(string StorageKey, string ThumbnailStorageKey, string ContentType, int Width, int Height);
public sealed record StoredAttachment(string StorageKey, string OriginalName, string ContentType, long Length, string Sha256);
public interface IFileStorageService
{
    Task<StoredImage> SaveImageAsync(FileUpload upload, string folder, CancellationToken ct);
    Task<StoredImage> SaveProductImageAsync(FileUpload upload, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
    Task<StoredAttachment> SaveAttachmentAsync(FileUpload upload, CancellationToken ct);
}
