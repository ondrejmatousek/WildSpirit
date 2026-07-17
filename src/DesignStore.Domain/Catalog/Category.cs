using DesignStore.Domain.Common;

namespace DesignStore.Domain.Catalog;

public sealed class Category : AuditableEntity
{
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public List<Category> Children { get; set; } = [];
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public List<CategoryTranslation> Translations { get; set; } = [];
    public List<Product> Products { get; set; } = [];
}

public sealed class CategoryTranslation : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
}
