using DesignStore.Application.Abstractions.Files;
using DesignStore.Domain.Catalog;
using FluentValidation;

namespace DesignStore.Application.Catalog;

public sealed record AdminProductListItem(Guid Id, string Sku, string Name, decimal Price, AvailabilityStatus Availability, bool IsPublished, int StockQuantity, bool HasVariants);
public sealed record AdminLookup(Guid Id, string Name);
public sealed record ProductImageAdminItem(Guid Id, string Url, string ThumbnailUrl, string AltText, bool IsPrimary, int DisplayOrder, int Width, int Height);

public sealed class ProductTranslationInput
{
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

public sealed class ProductVariantInput
{
    public Guid? Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameDe { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.InStock;
    public int StockQuantity { get; set; }
    public bool CanBeMadeToOrder { get; set; }
    public int? EstimatedProductionDays { get; set; }
}

public sealed class ProductOptionValueInput
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceAdjustment { get; set; }
    public int AdditionalProductionDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductOptionInput
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProductOptionType Type { get; set; } = ProductOptionType.Select;
    public bool IsRequired { get; set; }
    public int? MaximumTextLength { get; set; }
    public List<ProductOptionValueInput> Values { get; set; } = [];
}

public sealed class EditProductRequest
{
    public Guid? Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? CollectionId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? StartingPrice { get; set; }
    public decimal VatRate { get; set; } = 21;
    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.InStock;
    public bool TrackInventory { get; set; } = true;
    public int StockQuantity { get; set; }
    public bool CanBeMadeToOrder { get; set; }
    public bool AutomaticallySwitchToMadeToOrder { get; set; }
    public int? EstimatedProductionDays { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsLimited { get; set; }
    public bool HasVariants { get; set; }
    public int DisplayOrder { get; set; }
    public List<Guid> MaterialIds { get; set; } = [];
    public List<ProductTranslationInput> Translations { get; set; } = [new()];
    public List<ProductVariantInput> Variants { get; set; } = [];
    public List<ProductOptionInput> Options { get; set; } = [];
    public List<ProductImageAdminItem> Images { get; set; } = [];
}

public sealed class EditCategoryRequest
{
    public Guid? Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public List<CategoryTranslationInput> Translations { get; set; } = [new()];
}

public sealed class CategoryTranslationInput
{
    public string Culture { get; set; } = "cs-CZ";
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
}

public sealed class ReferenceItemRequest
{
    public Guid? Id { get; set; }
    public string Type { get; set; } = "material";
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameDe { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public interface ICatalogAdminService
{
    Task<IReadOnlyList<AdminProductListItem>> GetProductsAsync(CancellationToken ct);
    Task<string> GetNextProductSkuAsync(CancellationToken ct);
    Task<EditProductRequest?> GetProductAsync(Guid id, CancellationToken ct);
    Task<Guid> SaveProductAsync(EditProductRequest request, string? actor, CancellationToken ct);
    Task ArchiveProductAsync(Guid id, string? actor, CancellationToken ct);
    Task<IReadOnlyList<EditCategoryRequest>> GetCategoriesAsync(CancellationToken ct);
    Task<EditCategoryRequest?> GetCategoryAsync(Guid id, CancellationToken ct);
    Task<Guid> SaveCategoryAsync(EditCategoryRequest request, string? actor, CancellationToken ct);
    Task<IReadOnlyList<AdminLookup>> GetCollectionsAsync(CancellationToken ct);
    Task<IReadOnlyList<AdminLookup>> GetMaterialsAsync(CancellationToken ct);
    Task<IReadOnlyList<AdminLookup>> GetColorsAsync(CancellationToken ct);
    Task<Guid> SaveReferenceAsync(ReferenceItemRequest request, string? actor, CancellationToken ct);
    Task<Guid> AddProductImageAsync(Guid productId, StoredImage image, string altText, bool isPrimary, string? actor, CancellationToken ct);
    Task<ProductImageFile?> GetProductImageAsync(Guid imageId, bool thumbnail, CancellationToken ct);
}
public sealed record ProductImageFile(string StorageKey, string ContentType);

public sealed class EditProductRequestValidator : AbstractValidator<EditProductRequest>
{
    private static readonly string[] Cultures = ["cs-CZ", "en", "de"];
    public EditProductRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64).Matches("^[A-Za-z0-9._-]+$");
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.AvailabilityStatus != AvailabilityStatus.MadeToOrder || x.CanBeMadeToOrder).WithMessage("Produkt na objednávku musí mít povolenou opětovnou výrobu.");
        RuleFor(x => x).Must(x => !x.HasVariants || x.Variants.Count > 0).WithMessage("Variantový produkt musí obsahovat alespoň jednu variantu.");
        RuleFor(x => x).Must(x => !x.HasVariants || x.StockQuantity == 0).WithMessage("Sklad variantového produktu se vede pouze na variantách.");
        RuleFor(x => x.Translations).NotEmpty().Must(x => x.Select(t => t.Culture).Distinct().Count() == x.Count).WithMessage("Každá kultura smí být uvedena pouze jednou.");
        RuleForEach(x => x.Translations).ChildRules(t => { t.RuleFor(x => x.Culture).Must(Cultures.Contains); t.RuleFor(x => x.Name).NotEmpty().MaximumLength(200); t.RuleFor(x => x.Slug).NotEmpty().MaximumLength(220).Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$"); t.RuleFor(x => x.LongDescription).Must(IsPlainText).WithMessage("HTML není v popisu povoleno."); t.RuleFor(x => x.ShortDescription).Must(IsPlainText).WithMessage("HTML není v popisu povoleno."); t.RuleFor(x => x.SeoTitle).MaximumLength(70); t.RuleFor(x => x.SeoDescription).MaximumLength(170); });
        RuleForEach(x => x.Variants).ChildRules(v => { v.RuleFor(x => x.Sku).NotEmpty().MaximumLength(64); v.RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0); });
        RuleForEach(x => x.Options).ChildRules(o => { o.RuleFor(x => x.Name).NotEmpty().MaximumLength(160); o.RuleFor(x => x.MaximumTextLength).InclusiveBetween(1, 500).When(x => x.MaximumTextLength.HasValue); });
    }
    private static bool IsPlainText(string? value) => string.IsNullOrEmpty(value) || (!value.Contains('<') && !value.Contains('>'));
}

public sealed class EditCategoryRequestValidator : AbstractValidator<EditCategoryRequest>
{
    public EditCategoryRequestValidator()
    {
        RuleFor(x => x).Must(x => !x.Id.HasValue || !x.ParentCategoryId.HasValue || x.Id.Value != x.ParentCategoryId.Value).WithMessage("Kategorie nemůže být sama sobě nadřazená.");
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.BannerUrl).MaximumLength(500);
        RuleFor(x => x.Translations).NotEmpty();
        RuleForEach(x => x.Translations).ChildRules(t => { t.RuleFor(x => x.Culture).Must(c => c is "cs-CZ" or "en" or "de"); t.RuleFor(x => x.Name).NotEmpty().MaximumLength(160); t.RuleFor(x => x.Slug).NotEmpty().Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$"); t.RuleFor(x => x.LongDescription).Must(v => string.IsNullOrEmpty(v) || (!v.Contains('<') && !v.Contains('>'))).WithMessage("HTML není v popisu povoleno."); });
    }
}
