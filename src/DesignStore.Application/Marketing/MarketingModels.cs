namespace DesignStore.Application.Marketing;

public sealed record ContentPageView(Guid Id, string Title, string Slug, string Summary, string BodyHtml, string? SeoTitle, string? SeoDescription);
public sealed record GalleryItemView(string Title, string Description, string ImageUrl);
public sealed record ReviewView(string AuthorName, int Rating, string Text);
public sealed record NewsletterResult(bool AlreadyConfirmed, string DevelopmentConfirmationToken, string DevelopmentUnsubscribeToken);
public sealed record SitemapUrl(string Location, DateTime? LastModifiedUtc);

public sealed class NewsletterSubscribeRequest
{
    public string Email { get; set; } = "";
    public string Culture { get; set; } = "cs-CZ";
    public bool ConsentAccepted { get; set; }
}

public sealed class ContactRequest
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string Culture { get; set; } = "cs-CZ";
    public string? IpAddress { get; set; }
}

public interface IMarketingService
{
    Task<ContentPageView?> GetPageAsync(string culture, string slug, CancellationToken ct);
    Task<IReadOnlyList<GalleryItemView>> GetGalleryAsync(string culture, CancellationToken ct);
    Task<IReadOnlyList<ReviewView>> GetReviewsAsync(string culture, CancellationToken ct);
    Task<NewsletterResult> SubscribeAsync(NewsletterSubscribeRequest request, CancellationToken ct);
    Task<bool> ConfirmNewsletterAsync(string token, CancellationToken ct);
    Task<bool> UnsubscribeNewsletterAsync(string token, CancellationToken ct);
    Task<Guid> SendContactAsync(ContactRequest request, CancellationToken ct);
    Task<IReadOnlyList<SitemapUrl>> GetSitemapAsync(string baseUrl, CancellationToken ct);
    Task<string> ExportNewsletterCsvAsync(CancellationToken ct);
}
