using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Identity;
using DesignStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Tests.Infrastructure;

public sealed class ModelTests
{
    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql("Host=localhost;Database=model_metadata_only").Options);

    [Fact]
    public void Model_contains_catalog_and_identity_with_concurrency_tokens()
    {
        using var db = CreateDb();
        Assert.NotNull(db.Model.FindEntityType(typeof(Product)));
        Assert.NotNull(db.Model.FindEntityType(typeof(ApplicationUser)));
        Assert.True(db.Model.FindEntityType(typeof(Product))!.FindProperty("Version")!.IsConcurrencyToken);
        Assert.True(db.Model.FindEntityType(typeof(DesignStore.Domain.Commerce.Order))!.FindProperty("Version")!.IsConcurrencyToken);
    }

    [Fact]
    public void Product_sku_and_localized_slug_are_unique_in_model()
    {
        using var db = CreateDb();
        Assert.Contains(db.Model.FindEntityType(typeof(Product))!.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(Product.Sku)]));
        Assert.Contains(db.Model.FindEntityType(typeof(ProductTranslation))!.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(ProductTranslation.Culture), nameof(ProductTranslation.Slug)]));
    }
}
