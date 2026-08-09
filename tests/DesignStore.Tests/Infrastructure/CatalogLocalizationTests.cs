using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Catalog;

namespace DesignStore.Tests.Infrastructure;

[Collection("SqlServer database")]
public sealed class CatalogLocalizationTests(SqlServerDatabaseFixture database)
{
    [Fact]
    public async Task Missing_requested_translation_falls_back_to_czech_in_sql_server()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Nábytek", Slug = "nabytek-test", ShortDescription = "Popis" }] };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await new CatalogService(db).GetCategoriesAsync("de", default);

        Assert.Equal("Nábytek", result.Single(x => x.Id == category.Id).Name);
    }

    [Fact]
    public async Task Product_listing_orders_in_sql_and_localizes_after_materialization()
    {
        await using var db = database.CreateDb();
        var category = new Category
        {
            DisplayOrder = 20,
            Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Světla", Slug = "svetla-query-test", ShortDescription = "Popis" }]
        };
        category.Products.Add(new Product
        {
            Sku = "QUERY-TRANSLATION-001",
            BasePrice = 1000,
            IsPublished = true,
            DisplayOrder = 1,
            AvailabilityStatus = AvailabilityStatus.InStock,
            StockQuantity = 1,
            Translations = [new ProductTranslation { Culture = "cs-CZ", Name = "Testovací světlo", Slug = "testovaci-svetlo", ShortDescription = "Popis" }]
        });
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await new CatalogService(db).GetProductsAsync("de", category.Id, default);

        Assert.Equal("Testovací světlo", Assert.Single(result).Name);
    }
}
