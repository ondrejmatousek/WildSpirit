using DesignStore.Domain.Common;

namespace DesignStore.Domain.Catalog;

public enum ProductOptionType { Select = 1, Text = 2, Engraving = 3 }

public sealed class ProductOption : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public ProductOptionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public int? MaximumTextLength { get; set; }
    public List<ProductOptionTranslation> Translations { get; set; } = [];
    public List<ProductOptionValue> Values { get; set; } = [];
}

public sealed class ProductOptionTranslation : AuditableEntity
{
    public Guid ProductOptionId { get; set; }
    public ProductOption ProductOption { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class ProductOptionValue : AuditableEntity
{
    public Guid ProductOptionId { get; set; }
    public ProductOption ProductOption { get; set; } = null!;
    public decimal PriceAdjustment { get; set; }
    public int AdditionalProductionDays { get; set; }
    public string ImageStorageKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public List<ProductOptionValueTranslation> Translations { get; set; } = [];
    public List<ProductVariantOptionValue> Variants { get; set; } = [];
}

public sealed class ProductOptionValueTranslation : AuditableEntity
{
    public Guid ProductOptionValueId { get; set; }
    public ProductOptionValue ProductOptionValue { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class ProductVariantOptionValue
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public Guid ProductOptionValueId { get; set; }
    public ProductOptionValue ProductOptionValue { get; set; } = null!;
}

public static class ProductPriceCalculator
{
    public static decimal Calculate(decimal basePrice, IEnumerable<ProductOptionValue> values) =>
        decimal.Round(basePrice + values.Sum(x => x.PriceAdjustment), 2, MidpointRounding.AwayFromZero);

    public static int CalculateProductionDays(int baseDays, IEnumerable<ProductOptionValue> values) =>
        baseDays + values.Sum(x => x.AdditionalProductionDays);
}
