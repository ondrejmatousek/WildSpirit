using DesignStore.Domain.Common;

namespace DesignStore.Domain.Catalog;

public sealed class Product : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? CollectionId { get; set; }
    public Collection? Collection { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal? StartingPrice { get; set; }
    public decimal VatRate { get; set; } = 21m;
    public AvailabilityStatus AvailabilityStatus { get; set; }
    public bool TrackInventory { get; set; } = true;
    public int StockQuantity { get; set; }
    public bool CanBeMadeToOrder { get; set; }
    public bool AutomaticallySwitchToMadeToOrder { get; set; }
    public int? EstimatedProductionDays { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsLimited { get; set; }
    public bool HasVariants { get; set; }
    public int DisplayOrder { get; set; }
    public string MainImageUrl { get; set; } = string.Empty;
    public List<ProductTranslation> Translations { get; set; } = [];
    public List<ProductVariant> Variants { get; set; } = [];
    public List<ProductImage> Images { get; set; } = [];
    public List<ProductMaterial> ProductMaterials { get; set; } = [];
    public List<ProductOption> Options { get; set; } = [];
    public List<ProductRelatedItem> RelatedItems { get; set; } = [];
    public List<ProductSlugHistory> SlugHistory { get; set; } = [];

    public bool CanAddToCart(int quantity = 1) => IsPublished && quantity > 0 &&
        (HasVariants
            ? Variants.Any(variant =>
                variant.AvailabilityStatus == AvailabilityStatus.InStock &&
                variant.StockQuantity >= quantity)
            : AvailabilityStatus == AvailabilityStatus.InStock && (!TrackInventory || StockQuantity >= quantity));

    public bool CanRequestProduction() =>
        IsPublished && AvailabilityStatus == AvailabilityStatus.MadeToOrder && CanBeMadeToOrder;
}

public sealed class ProductTranslation : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public string Materials { get; set; } = string.Empty;
    public string Dimensions { get; set; } = string.Empty;
    public string CareInstructions { get; set; } = string.Empty;
    public string ShippingInformation { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public string OpenGraphTitle { get; set; } = string.Empty;
    public string OpenGraphDescription { get; set; } = string.Empty;
}

public sealed class ProductVariant : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public AvailabilityStatus AvailabilityStatus { get; set; }
    public int StockQuantity { get; set; }
    public bool CanBeMadeToOrder { get; set; }
    public int? EstimatedProductionDays { get; set; }
    public List<ProductVariantTranslation> Translations { get; set; } = [];
    public List<ProductVariantOptionValue> OptionValues { get; set; } = [];
}

public sealed class ProductVariantTranslation : AuditableEntity
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
