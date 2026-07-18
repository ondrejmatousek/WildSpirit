using DesignStore.Domain.Common;

namespace DesignStore.Domain.Marketing;

public enum ContentPageType { Standard = 0, Story = 1, Craft = 2, Privacy = 3, Terms = 4 }
public enum NewsletterStatus { Pending = 0, Confirmed = 1, Unsubscribed = 2 }
public enum ContactMessageStatus { New = 0, Reviewed = 1, Closed = 2, Spam = 3 }

public sealed class ContentPage : AuditableEntity
{
    public ContentPageType Type { get; set; }
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public List<ContentPageTranslation> Translations { get; set; } = [];
}

public sealed class ContentPageTranslation : AuditableEntity
{
    public Guid ContentPageId { get; set; }
    public ContentPage ContentPage { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Summary { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}

public sealed class GalleryItem : AuditableEntity
{
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public string ImageUrl { get; set; } = "";
    public List<GalleryItemTranslation> Translations { get; set; } = [];
}

public sealed class GalleryItemTranslation : AuditableEntity
{
    public Guid GalleryItemId { get; set; }
    public GalleryItem GalleryItem { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class Review : AuditableEntity
{
    public bool IsPublished { get; set; }
    public int Rating { get; set; } = 5;
    public string AuthorName { get; set; } = "";
    public string Culture { get; set; } = "cs-CZ";
    public string Text { get; set; } = "";
}

public sealed class NewsletterSubscription : AuditableEntity
{
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string Culture { get; set; } = "cs-CZ";
    public NewsletterStatus Status { get; set; } = NewsletterStatus.Pending;
    public string ConfirmationTokenHash { get; set; } = "";
    public string UnsubscribeTokenHash { get; set; } = "";
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? UnsubscribedAtUtc { get; set; }
    public DateTime? LastConfirmationSentAtUtc { get; set; }
}

public sealed class ContactMessage : AuditableEntity
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string Culture { get; set; } = "cs-CZ";
    public string? IpHash { get; set; }
    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;
}

public static class MarketingRules
{
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
    public static bool CanSubmitContact(int recentCount) => recentCount < 3;
}
