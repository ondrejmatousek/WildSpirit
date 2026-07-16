using DesignStore.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesignStore.Infrastructure.Persistence;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasOne(x => x.ParentCategory).WithMany(x => x.Children).HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ParentCategoryId, x.DisplayOrder });
    }
}

internal sealed class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> b)
    {
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.Name).HasMaxLength(160);
        b.Property(x => x.Slug).HasMaxLength(180);
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.SeoTitle).HasMaxLength(70);
        b.Property(x => x.SeoDescription).HasMaxLength(170);
        b.HasIndex(x => new { x.CategoryId, x.Culture }).IsUnique();
        b.HasIndex(x => new { x.Culture, x.Slug }).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.Sku).HasMaxLength(64);
        b.Property(x => x.BasePrice).HasPrecision(18, 2);
        b.Property(x => x.StartingPrice).HasPrecision(18, 2);
        b.Property(x => x.VatRate).HasPrecision(5, 2);
        b.Property(x => x.MainImageUrl).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Sku).IsUnique();
        b.HasIndex(x => new { x.IsPublished, x.AvailabilityStatus, x.DisplayOrder });
        b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> b)
    {
        b.Property(x => x.Culture).HasMaxLength(10);
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Slug).HasMaxLength(220);
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.SeoTitle).HasMaxLength(70);
        b.Property(x => x.SeoDescription).HasMaxLength(170);
        b.HasIndex(x => new { x.ProductId, x.Culture }).IsUnique();
        b.HasIndex(x => new { x.Culture, x.Slug }).IsUnique();
    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.Property(x => x.Sku).HasMaxLength(64);
        b.Property(x => x.Name).HasMaxLength(160);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Sku).IsUnique();
    }
}
