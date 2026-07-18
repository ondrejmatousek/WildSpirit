using DesignStore.Domain.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesignStore.Infrastructure.Persistence;

internal sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> b)
    {
        b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
    }
}

internal sealed class ContentPageTranslationConfiguration : IEntityTypeConfiguration<ContentPageTranslation>
{
    public void Configure(EntityTypeBuilder<ContentPageTranslation> b)
    {
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.Title).HasMaxLength(180);
        b.Property(x => x.Slug).HasMaxLength(200);
        b.Property(x => x.Summary).HasMaxLength(500);
        b.Property(x => x.SeoTitle).HasMaxLength(70);
        b.Property(x => x.SeoDescription).HasMaxLength(170);
        b.HasIndex(x => new { x.ContentPageId, x.Culture }).IsUnique();
        b.HasIndex(x => new { x.Culture, x.Slug }).IsUnique();
        b.HasOne(x => x.ContentPage).WithMany(x => x.Translations).HasForeignKey(x => x.ContentPageId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GalleryItemConfiguration : IEntityTypeConfiguration<GalleryItem>
{
    public void Configure(EntityTypeBuilder<GalleryItem> b)
    {
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
    }
}

internal sealed class GalleryItemTranslationConfiguration : IEntityTypeConfiguration<GalleryItemTranslation>
{
    public void Configure(EntityTypeBuilder<GalleryItemTranslation> b)
    {
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.Title).HasMaxLength(180);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.GalleryItemId, x.Culture }).IsUnique();
        b.HasOne(x => x.GalleryItem).WithMany(x => x.Translations).HasForeignKey(x => x.GalleryItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.Property(x => x.AuthorName).HasMaxLength(160);
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.Text).HasMaxLength(2000);
        b.HasIndex(x => new { x.IsPublished, x.Culture, x.CreatedAtUtc });
    }
}

internal sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscription> b)
    {
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.NormalizedEmail).HasMaxLength(320);
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.ConfirmationTokenHash).HasMaxLength(128);
        b.Property(x => x.UnsubscribeTokenHash).HasMaxLength(128);
        b.HasIndex(x => x.NormalizedEmail).IsUnique();
        b.HasIndex(x => x.ConfirmationTokenHash).IsUnique();
        b.HasIndex(x => x.UnsubscribeTokenHash).IsUnique();
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.FullName).HasMaxLength(160);
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Message).HasMaxLength(5000);
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.IpHash).HasMaxLength(128);
        b.HasIndex(x => new { x.Email, x.CreatedAtUtc });
        b.HasIndex(x => new { x.IpHash, x.CreatedAtUtc });
    }
}
