using DesignStore.Domain.Common;

namespace DesignStore.Domain.Catalog;

public sealed class Collection : AuditableEntity
{
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime? AvailableFromUtc { get; set; }
    public DateTime? AvailableToUtc { get; set; }
    public List<CollectionTranslation> Translations { get; set; } = [];
    public List<Product> Products { get; set; } = [];
}

public sealed class CollectionTranslation : AuditableEntity
{
    public Guid CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class Material : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<MaterialTranslation> Translations { get; set; } = [];
    public List<ProductMaterial> ProductMaterials { get; set; } = [];
}

public sealed class MaterialTranslation : AuditableEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class Color : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<ColorTranslation> Translations { get; set; } = [];
}

public sealed class ColorTranslation : AuditableEntity
{
    public Guid ColorId { get; set; }
    public Color Color { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
}

public sealed class ProductMaterial
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public string Note { get; set; } = string.Empty;
}
