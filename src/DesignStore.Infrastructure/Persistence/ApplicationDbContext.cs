using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
