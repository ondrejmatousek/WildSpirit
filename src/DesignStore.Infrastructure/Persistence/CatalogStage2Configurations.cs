using DesignStore.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesignStore.Infrastructure.Persistence;

internal sealed class ProductVariantTranslationConfiguration : IEntityTypeConfiguration<ProductVariantTranslation>
{
    public void Configure(EntityTypeBuilder<ProductVariantTranslation> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.Name).HasMaxLength(160); b.Property(x => x.Description).HasMaxLength(1000); b.HasIndex(x => new { x.ProductVariantId, x.Culture }).IsUnique(); }
}
internal sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> b) => b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
}
internal sealed class CollectionTranslationConfiguration : IEntityTypeConfiguration<CollectionTranslation>
{
    public void Configure(EntityTypeBuilder<CollectionTranslation> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.Name).HasMaxLength(160); b.Property(x => x.Slug).HasMaxLength(180); b.Property(x => x.Description).HasMaxLength(4000); b.HasIndex(x => new { x.CollectionId, x.Culture }).IsUnique(); b.HasIndex(x => new { x.Culture, x.Slug }).IsUnique(); }
}
internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> b) { b.Property(x => x.Code).HasMaxLength(64); b.HasIndex(x => x.Code).IsUnique(); }
}
internal sealed class MaterialTranslationConfiguration : IEntityTypeConfiguration<MaterialTranslation>
{
    public void Configure(EntityTypeBuilder<MaterialTranslation> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.Name).HasMaxLength(120); b.Property(x => x.Description).HasMaxLength(2000); b.HasIndex(x => new { x.MaterialId, x.Culture }).IsUnique(); }
}
internal sealed class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> b) { b.Property(x => x.Code).HasMaxLength(64); b.Property(x => x.Hex).HasMaxLength(7); b.HasIndex(x => x.Code).IsUnique(); }
}
internal sealed class ColorTranslationConfiguration : IEntityTypeConfiguration<ColorTranslation>
{
    public void Configure(EntityTypeBuilder<ColorTranslation> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.Name).HasMaxLength(120); b.HasIndex(x => new { x.ColorId, x.Culture }).IsUnique(); }
}
internal sealed class ProductMaterialConfiguration : IEntityTypeConfiguration<ProductMaterial>
{
    public void Configure(EntityTypeBuilder<ProductMaterial> b) { b.HasKey(x => new { x.ProductId, x.MaterialId }); b.Property(x => x.Note).HasMaxLength(500); b.HasOne(x => x.Product).WithMany(x => x.ProductMaterials).HasForeignKey(x => x.ProductId); b.HasOne(x => x.Material).WithMany(x => x.ProductMaterials).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict); }
}
internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b) { b.Property(x => x.StorageKey).HasMaxLength(500); b.Property(x => x.ThumbnailStorageKey).HasMaxLength(500); b.Property(x => x.ContentType).HasMaxLength(100); b.Property(x => x.AltText).HasMaxLength(300); b.HasIndex(x => new { x.ProductId, x.DisplayOrder }); b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.NoAction); }
}
internal sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> b) { b.HasIndex(x => new { x.ProductId, x.DisplayOrder }); }
}
internal sealed class ProductOptionTranslationConfiguration : IEntityTypeConfiguration<ProductOptionTranslation>
{
    public void Configure(EntityTypeBuilder<ProductOptionTranslation> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.Name).HasMaxLength(160); b.Property(x => x.Description).HasMaxLength(1000); b.HasIndex(x => new { x.ProductOptionId, x.Culture }).IsUnique(); }
}
internal sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> b) { b.Property(x => x.PriceAdjustment).HasPrecision(18, 2); b.Property(x => x.ImageStorageKey).HasMaxLength(500); b.HasIndex(x => new { x.ProductOptionId, x.DisplayOrder }); }
}
internal sealed class ProductOptionValueTranslationConfiguration : IEntityTypeConfiguration<ProductOptionValueTranslation>
{
    public void Configure(EntityTypeBuilder<ProductOptionValueTranslation> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.Name).HasMaxLength(160); b.Property(x => x.Description).HasMaxLength(1000); b.HasIndex(x => new { x.ProductOptionValueId, x.Culture }).IsUnique(); }
}
internal sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> b) { b.HasKey(x => new { x.ProductVariantId, x.ProductOptionValueId }); b.HasOne(x => x.ProductVariant).WithMany(x => x.OptionValues).HasForeignKey(x => x.ProductVariantId); b.HasOne(x => x.ProductOptionValue).WithMany(x => x.Variants).HasForeignKey(x => x.ProductOptionValueId).OnDelete(DeleteBehavior.NoAction); }
}
internal sealed class ProductRelatedItemConfiguration : IEntityTypeConfiguration<ProductRelatedItem>
{
    public void Configure(EntityTypeBuilder<ProductRelatedItem> b) { b.HasKey(x => new { x.ProductId, x.RelatedProductId }); b.HasOne(x => x.Product).WithMany(x => x.RelatedItems).HasForeignKey(x => x.ProductId); b.HasOne(x => x.RelatedProduct).WithMany().HasForeignKey(x => x.RelatedProductId).OnDelete(DeleteBehavior.NoAction); }
}
internal sealed class ProductSlugHistoryConfiguration : IEntityTypeConfiguration<ProductSlugHistory>
{
    public void Configure(EntityTypeBuilder<ProductSlugHistory> b) { b.Property(x => x.Culture).HasMaxLength(10); b.Property(x => x.OldSlug).HasMaxLength(220); b.HasIndex(x => new { x.Culture, x.OldSlug }).IsUnique(); }
}
