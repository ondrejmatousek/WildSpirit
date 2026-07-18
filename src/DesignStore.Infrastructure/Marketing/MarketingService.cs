using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DesignStore.Application.Marketing;
using DesignStore.Domain.Commerce;
using DesignStore.Domain.Marketing;
using DesignStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Infrastructure.Marketing;

public sealed class MarketingService(ApplicationDbContext db) : IMarketingService
{
    public async Task<ContentPageView?> GetPageAsync(string culture, string slug, CancellationToken ct)
    {
        var preferred = await db.ContentPageTranslations.AsNoTracking()
            .Where(x => x.ContentPage.IsPublished && x.Culture == culture && x.Slug == slug)
            .Select(x => new ContentPageView(x.ContentPageId, x.Title, x.Slug, x.Summary, x.BodyHtml, x.SeoTitle, x.SeoDescription))
            .FirstOrDefaultAsync(ct);

        if (preferred is not null) return preferred;

        return await db.ContentPageTranslations.AsNoTracking()
            .Where(x => x.ContentPage.IsPublished && x.Culture == "cs-CZ" && x.Slug == slug)
            .Select(x => new ContentPageView(x.ContentPageId, x.Title, x.Slug, x.Summary, x.BodyHtml, x.SeoTitle, x.SeoDescription))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<GalleryItemView>> GetGalleryAsync(string culture, CancellationToken ct) => await db.GalleryItems.AsNoTracking()
        .Where(x => x.IsPublished).OrderBy(x => x.DisplayOrder)
        .Select(x => new GalleryItemView(
            x.Translations.Where(t => t.Culture == culture).Select(t => t.Title).FirstOrDefault() ?? x.Translations.Select(t => t.Title).First(),
            x.Translations.Where(t => t.Culture == culture).Select(t => t.Description).FirstOrDefault() ?? x.Translations.Select(t => t.Description).First(),
            x.ImageUrl)).ToListAsync(ct);

    public async Task<IReadOnlyList<ReviewView>> GetReviewsAsync(string culture, CancellationToken ct) => await db.Reviews.AsNoTracking()
        .Where(x => x.IsPublished && x.Culture == culture).OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new ReviewView(x.AuthorName, x.Rating, x.Text)).ToListAsync(ct);

    public async Task<NewsletterResult> SubscribeAsync(NewsletterSubscribeRequest request, CancellationToken ct)
    {
        if (!request.ConsentAccepted) throw new InvalidOperationException("Souhlas se zpracováním je povinný.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) throw new InvalidOperationException("E-mail není platný.");
        var normalized = MarketingRules.NormalizeEmail(request.Email);
        var confirmationToken = Token();
        var unsubscribeToken = Token();
        var item = await db.NewsletterSubscriptions.FirstOrDefaultAsync(x => x.NormalizedEmail == normalized, ct);
        if (item is null)
        {
            item = new NewsletterSubscription { Email = request.Email.Trim(), NormalizedEmail = normalized };
            db.NewsletterSubscriptions.Add(item);
        }
        if (item.Status == NewsletterStatus.Confirmed) return new NewsletterResult(true, "", "");
        item.Culture = request.Culture;
        item.Status = NewsletterStatus.Pending;
        item.ConfirmationTokenHash = Hash(confirmationToken);
        item.UnsubscribeTokenHash = Hash(unsubscribeToken);
        item.LastConfirmationSentAtUtc = DateTime.UtcNow;
        db.OutboxMessages.Add(new OutboxMessage { Type = "NewsletterConfirmation", Payload = JsonSerializer.Serialize(new { item.Email, request.Culture }) });
        await db.SaveChangesAsync(ct);
        return new NewsletterResult(false, confirmationToken, unsubscribeToken);
    }

    public async Task<bool> ConfirmNewsletterAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var item = await db.NewsletterSubscriptions.FirstOrDefaultAsync(x => x.ConfirmationTokenHash == hash, ct);
        if (item is null) return false;
        item.Status = NewsletterStatus.Confirmed;
        item.ConfirmedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UnsubscribeNewsletterAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var item = await db.NewsletterSubscriptions.FirstOrDefaultAsync(x => x.UnsubscribeTokenHash == hash, ct);
        if (item is null) return false;
        if (item.Status != NewsletterStatus.Unsubscribed)
        {
            item.Status = NewsletterStatus.Unsubscribed;
            item.UnsubscribedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<Guid> SendContactAsync(ContactRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Message)) throw new InvalidOperationException("E-mail a zpráva jsou povinné.");
        var since = DateTime.UtcNow.AddHours(-1);
        var ipHash = string.IsNullOrWhiteSpace(request.IpAddress) ? null : Hash(request.IpAddress);
        var recent = await db.ContactMessages.CountAsync(x => x.CreatedAtUtc >= since && (x.Email == request.Email || (ipHash != null && x.IpHash == ipHash)), ct);
        if (!MarketingRules.CanSubmitContact(recent)) throw new InvalidOperationException("Kontakt byl dočasně omezen.");
        var entity = new ContactMessage { Email = request.Email.Trim(), FullName = request.FullName.Trim(), Subject = request.Subject.Trim(), Message = request.Message.Trim(), Culture = request.Culture, IpHash = ipHash };
        db.ContactMessages.Add(entity);
        db.OutboxMessages.Add(new OutboxMessage { Type = "ContactMessage", Payload = JsonSerializer.Serialize(new { entity.Email, entity.Subject }) });
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<IReadOnlyList<SitemapUrl>> GetSitemapAsync(string baseUrl, CancellationToken ct)
    {
        baseUrl = baseUrl.TrimEnd('/');
        var pages = await db.ContentPageTranslations.AsNoTracking().Where(x => x.ContentPage.IsPublished)
            .Select(x => new SitemapUrl($"{baseUrl}/{x.Culture}/Page/{x.Slug}", x.UpdatedAtUtc ?? x.CreatedAtUtc)).ToListAsync(ct);
        pages.Add(new SitemapUrl($"{baseUrl}/cs-CZ", null));
        pages.Add(new SitemapUrl($"{baseUrl}/cs-CZ/Catalog", null));
        return pages.OrderBy(x => x.Location).ToList();
    }

    public async Task<string> ExportNewsletterCsvAsync(CancellationToken ct)
    {
        var rows = await db.NewsletterSubscriptions.AsNoTracking().Where(x => x.Status == NewsletterStatus.Confirmed).OrderBy(x => x.Email).ToListAsync(ct);
        var sb = new StringBuilder("Email,Culture,ConfirmedAtUtc\r\n");
        foreach (var row in rows) sb.Append(row.Email).Append(',').Append(row.Culture).Append(',').Append(row.ConfirmedAtUtc?.ToString("O")).Append("\r\n");
        return sb.ToString();
    }

    private static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
