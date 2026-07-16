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
        var products = await ProductQuery(culture).OrderBy(x => x.Product.DisplayOrder).Take(20).ToListAsync(ct);
        var cards = products.Select(ToCard).ToList();
        return new(categories, cards.Where((_, i) => products[i].Product.IsFeatured).Take(6).ToList(),
            cards.Where((_, i) => products[i].Product.IsNew).Take(6).ToList(),
            cards.Where((_, i) => products[i].Product.IsLimited).Take(6).ToList());
    }

    public async Task<IReadOnlyList<ProductCard>> GetProductsAsync(string culture, Guid? categoryId, CancellationToken ct)
    {
        var query = ProductQuery(culture);
        if (categoryId.HasValue) query = query.Where(x => x.Product.CategoryId == categoryId.Value);
        var rows = await query.OrderBy(x => x.Product.DisplayOrder).ThenBy(x => x.Translation.Name).ToListAsync(ct);
        return rows.Select(ToCard).ToList();
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
        var row = await ProductQuery(culture).FirstOrDefaultAsync(x => x.Translation.Slug == slug, ct);
        if (row is null) return null;
        var p = row.Product;
        return new(p.Id, row.Translation.Name, row.Translation.Slug, row.Translation.ShortDescription,
            row.Translation.LongDescription, row.Translation.Story, row.Translation.Materials,
            row.Translation.Dimensions, row.Translation.CareInstructions, row.Translation.ShippingInformation,
            row.CategoryName, p.BasePrice, p.StartingPrice, p.AvailabilityStatus, p.EstimatedProductionDays,
            p.MainImageUrl, p.CanAddToCart(), p.CanRequestProduction(),
            p.Variants.OrderBy(v => v.Name).Select(v => new ProductVariantItem(v.Id, v.Name, v.Price ?? p.BasePrice,
                v.AvailabilityStatus, v.StockQuantity)).ToList());
    }

    private IQueryable<CatalogRow> ProductQuery(string culture)
    {
        var fallback = culture == "cs-CZ" ? "en" : "cs-CZ";
        return db.Products.AsNoTracking().Where(p => p.IsPublished)
            .Select(p => new CatalogRow(
                p,
                p.Translations.FirstOrDefault(t => t.Culture == culture)
                    ?? p.Translations.FirstOrDefault(t => t.Culture == fallback)
                    ?? p.Translations.First(),
                p.Category.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? p.Category.Translations.Select(t => t.Name).First()));
    }

    private static ProductCard ToCard(CatalogRow x) => new(x.Product.Id, x.Translation.Name,
        x.Translation.Slug, x.Translation.ShortDescription, x.CategoryName, x.Product.BasePrice,
        x.Product.StartingPrice, x.Product.AvailabilityStatus, x.Product.MainImageUrl,
        x.Product.IsNew, x.Product.IsLimited);

    private sealed record CatalogRow(Product Product, ProductTranslation Translation, string CategoryName);
}
