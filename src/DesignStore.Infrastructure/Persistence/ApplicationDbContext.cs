using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.Domain.Marketing;
using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DesignStore.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantTranslation> ProductVariantTranslations => Set<ProductVariantTranslation>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionTranslation> CollectionTranslations => Set<CollectionTranslation>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialTranslation> MaterialTranslations => Set<MaterialTranslation>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<ColorTranslation> ColorTranslations => Set<ColorTranslation>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductSlugHistory> ProductSlugHistories => Set<ProductSlugHistory>();
    public DbSet<Cart> Carts => Set<Cart>(); public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>(); public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>(); public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>(); public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>(); public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProductInquiry> ProductInquiries => Set<ProductInquiry>(); public DbSet<ProductInquiryConfigurationItem> ProductInquiryConfigurationItems => Set<ProductInquiryConfigurationItem>(); public DbSet<ProductInquiryAttachment> ProductInquiryAttachments => Set<ProductInquiryAttachment>(); public DbSet<ProductInquiryStatusHistory> ProductInquiryStatusHistories => Set<ProductInquiryStatusHistory>(); public DbSet<ProductInquiryNote> ProductInquiryNotes => Set<ProductInquiryNote>();
    public DbSet<Quote> Quotes => Set<Quote>(); public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>(); public DbSet<QuoteStatusHistory> QuoteStatusHistories => Set<QuoteStatusHistory>(); public DbSet<QuoteAccessToken> QuoteAccessTokens => Set<QuoteAccessToken>();
    public DbSet<ProductionJob> ProductionJobs => Set<ProductionJob>(); public DbSet<ProductionJobStatusHistory> ProductionJobStatusHistories => Set<ProductionJobStatusHistory>(); public DbSet<ProductionJobAttachment> ProductionJobAttachments => Set<ProductionJobAttachment>(); public DbSet<ProductionJobNote> ProductionJobNotes => Set<ProductionJobNote>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>(); public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>(); public DbSet<ContentPageTranslation> ContentPageTranslations => Set<ContentPageTranslation>();
    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>(); public DbSet<GalleryItemTranslation> GalleryItemTranslations => Set<GalleryItemTranslation>();
    public DbSet<Review> Reviews => Set<Review>(); public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>(); public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // The application keeps all times in UTC. PostgreSQL stores them as timestamptz; values whose Kind is
        // Unspecified (e.g. from admin forms) are treated as UTC, as they were with SQL Server datetime2.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
