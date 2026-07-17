using DesignStore.Domain.Common;

namespace DesignStore.Domain.Catalog;

public sealed class ProductImage : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ThumbnailStorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class ProductRelatedItem
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid RelatedProductId { get; set; }
    public Product RelatedProduct { get; set; } = null!;
    public int DisplayOrder { get; set; }
}

public sealed class ProductSlugHistory : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string OldSlug { get; set; } = string.Empty;
}
