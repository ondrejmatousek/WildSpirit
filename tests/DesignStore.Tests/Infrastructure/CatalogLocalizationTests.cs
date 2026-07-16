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

        Assert.Equal("Nábytek", Assert.Single(result).Name);
    }
}
