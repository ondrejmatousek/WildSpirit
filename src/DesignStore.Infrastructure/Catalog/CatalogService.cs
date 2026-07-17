using DesignStore.Application.Catalog;
using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Infrastructure.Catalog;

public sealed class CatalogService(ApplicationDbContext db) : ICatalogService
{
    public async Task<HomePageData> GetHomePageAsync(string culture, CancellationToken ct)
    {
        var categories = await GetCategoriesAsync(culture, ct);
        var products = await BaseProductQuery().OrderBy(p => p.DisplayOrder).Take(20).ToListAsync(ct);
        var rows = products.Select(p => ToRow(p, culture)).ToList();
        return new(categories,
            rows.Where(x => x.Product.IsFeatured).Take(6).Select(ToCard).ToList(),
            rows.Where(x => x.Product.IsNew).Take(6).Select(ToCard).ToList(),
            rows.Where(x => x.Product.IsLimited).Take(6).Select(ToCard).ToList());
    }

    public async Task<IReadOnlyList<ProductCard>> GetProductsAsync(string culture, Guid? categoryId, CancellationToken ct)
    {
        var query = BaseProductQuery();
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        var products = await query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Sku).ToListAsync(ct);
        return products.Select(p => ToCard(ToRow(p, culture))).ToList();
    }

    public async Task<IReadOnlyList<CategoryCard>> GetCategoriesAsync(string culture, CancellationToken ct)
    {
        var fallback = culture == "cs-CZ" ? "en" : "cs-CZ";
        return await db.Categories.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CategoryCard(x.Id,
                x.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == fallback).Select(t => t.Name).FirstOrDefault() ?? x.Translations.Select(t => t.Name).First(),
                x.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Culture == fallback).Select(t => t.Slug).FirstOrDefault() ?? x.Translations.Select(t => t.Slug).First(),
                x.Translations.Where(t => t.Culture == culture).Select(t => t.ShortDescription).FirstOrDefault()
                    ?? x.Translations.Select(t => t.ShortDescription).First(), x.ImageUrl))
            .ToListAsync(ct);
    }

    public async Task<ProductDetail?> GetProductAsync(string culture, string slug, CancellationToken ct)
    {
        var product = await BaseProductQuery()
            .FirstOrDefaultAsync(p => p.Translations.Any(t => t.Culture == culture && t.Slug == slug), ct);
        if (product is null) return null;
        var row = ToRow(product, culture);
        var t = row.Translation;
        return new(product.Id, t.Name, t.Slug, t.ShortDescription, t.LongDescription, t.Story,
            t.Materials, t.Dimensions, t.CareInstructions, t.ShippingInformation, row.CategoryName,
            product.BasePrice, product.StartingPrice, product.AvailabilityStatus, product.EstimatedProductionDays,
            product.MainImageUrl, product.CanAddToCart(), product.CanRequestProduction(),
            product.Variants.OrderBy(v => v.Name).Select(v => new ProductVariantItem(v.Id, v.Name,
                v.Price ?? product.BasePrice, v.AvailabilityStatus, v.StockQuantity)).ToList(),
            product.Images.OrderBy(x=>x.DisplayOrder).Select(x=>new ProductImageItem(x.Id,x.AltText,x.IsPrimary)).ToList(),
            product.Options.OrderBy(x=>x.DisplayOrder).Select(x=>new ProductOptionItem(x.Id,(x.Translations.FirstOrDefault(t=>t.Culture==culture)??x.Translations.First()).Name,x.Type,x.IsRequired,x.MaximumTextLength,x.Values.Where(v=>v.IsActive).OrderBy(v=>v.DisplayOrder).Select(v=>new ProductOptionValueItem(v.Id,(v.Translations.FirstOrDefault(t=>t.Culture==culture)??v.Translations.First()).Name,v.PriceAdjustment,v.AdditionalProductionDays)).ToList())).ToList());
    }

    public Task<string?> GetCurrentSlugAsync(string culture,string oldSlug,CancellationToken ct)=>db.ProductSlugHistories.AsNoTracking().Where(x=>x.Culture==culture&&x.OldSlug==oldSlug&&x.Product.IsPublished).Select(x=>x.Product.Translations.Where(t=>t.Culture==culture).Select(t=>t.Slug).FirstOrDefault()).FirstOrDefaultAsync(ct);

    private IQueryable<Product> BaseProductQuery() => db.Products.AsNoTracking()
        .Where(p => p.IsPublished)
        .Include(p => p.Translations)
        .Include(p => p.Category).ThenInclude(c => c.Translations)
        .Include(p => p.Variants)
        .Include(p => p.Images)
        .Include(p => p.Options).ThenInclude(o=>o.Translations)
        .Include(p => p.Options).ThenInclude(o=>o.Values).ThenInclude(v=>v.Translations)
        .AsSplitQuery();

    private static CatalogRow ToRow(Product product, string culture)
    {
        var fallback = culture == "cs-CZ" ? "en" : "cs-CZ";
        var translation = product.Translations.FirstOrDefault(t => t.Culture == culture)
            ?? product.Translations.FirstOrDefault(t => t.Culture == fallback)
            ?? product.Translations.First();
        var categoryName = product.Category.Translations.FirstOrDefault(t => t.Culture == culture)?.Name
            ?? product.Category.Translations.FirstOrDefault(t => t.Culture == fallback)?.Name
            ?? product.Category.Translations.First().Name;
        return new(product, translation, categoryName);
    }

    private static ProductCard ToCard(CatalogRow x) => new(x.Product.Id, x.Translation.Name,
        x.Translation.Slug, x.Translation.ShortDescription, x.CategoryName, x.Product.BasePrice,
        x.Product.StartingPrice, x.Product.AvailabilityStatus, x.Product.MainImageUrl,
        x.Product.IsNew, x.Product.IsLimited);

    private sealed record CatalogRow(Product Product, ProductTranslation Translation, string CategoryName);
}
