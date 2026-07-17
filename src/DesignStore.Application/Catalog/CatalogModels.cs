using DesignStore.Domain.Catalog;

namespace DesignStore.Application.Catalog;

public sealed record CategoryCard(Guid Id, string Name, string Slug, string Description, string ImageUrl);

public sealed record ProductCard(
    Guid Id, string Name, string Slug, string Description, string CategoryName, decimal Price,
    decimal? StartingPrice, AvailabilityStatus Availability, string ImageUrl, bool IsNew, bool IsLimited);

public sealed record ProductDetail(
    Guid Id, string Name, string Slug, string Description, string LongDescription, string Story,
    string Materials, string Dimensions, string Care, string Shipping, string CategoryName,
    decimal Price, decimal? StartingPrice, AvailabilityStatus Availability, int? ProductionDays,
    string ImageUrl, bool CanAddToCart, bool CanRequestProduction, IReadOnlyList<ProductVariantItem> Variants,
    IReadOnlyList<ProductImageItem> Images, IReadOnlyList<ProductOptionItem> Options);

public sealed record ProductVariantItem(Guid Id, string Name, decimal Price, AvailabilityStatus Availability, int StockQuantity);
public sealed record ProductImageItem(Guid Id, string AltText, bool IsPrimary);
public sealed record ProductOptionValueItem(Guid Id, string Name, decimal PriceAdjustment, int AdditionalProductionDays);
public sealed record ProductOptionItem(Guid Id, string Name, ProductOptionType Type, bool IsRequired, int? MaximumTextLength, IReadOnlyList<ProductOptionValueItem> Values);

public sealed record HomePageData(
    IReadOnlyList<CategoryCard> Categories,
    IReadOnlyList<ProductCard> Featured,
    IReadOnlyList<ProductCard> NewProducts,
    IReadOnlyList<ProductCard> Limited);

public interface ICatalogService
{
    Task<HomePageData> GetHomePageAsync(string culture, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductCard>> GetProductsAsync(string culture, Guid? categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryCard>> GetCategoriesAsync(string culture, CancellationToken cancellationToken);
    Task<ProductDetail?> GetProductAsync(string culture, string slug, CancellationToken cancellationToken);
    Task<string?> GetCurrentSlugAsync(string culture, string oldSlug, CancellationToken cancellationToken);
}
