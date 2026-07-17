using DesignStore.Application.Catalog;
using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Catalog;
using Microsoft.EntityFrameworkCore;
namespace DesignStore.Tests.Infrastructure;
[Collection("SqlServer database")]
public sealed class CatalogAdminSqlTests(SqlServerDatabaseFixture database)
{
    [Fact] public async Task Changing_slug_creates_redirect_history()
    {
        Guid id;string old;
        await using(var db=database.CreateDb())
        {
            var category=new Category{Translations=[new CategoryTranslation{Culture="cs-CZ",Name="Test",Slug=$"category-{Guid.NewGuid():N}",ShortDescription="Test"}]};db.Categories.Add(category);await db.SaveChangesAsync();
            var service=Service(db);var create=new EditProductRequest{Sku=$"SQL-{Guid.NewGuid():N}",CategoryId=category.Id,BasePrice=500,Translations=[new ProductTranslationInput{Culture="cs-CZ",Name="Produkt",Slug=$"old-{Guid.NewGuid():N}"}]};id=await service.SaveProductAsync(create,"test",default);old=create.Translations.Single().Slug;
        }
        await using(var db=database.CreateDb())
        {
            var service=Service(db);var edit=await service.GetProductAsync(id,default);edit!.Translations.Single().Slug=$"new-{Guid.NewGuid():N}";await service.SaveProductAsync(edit,"test",default);Assert.True(await db.ProductSlugHistories.AnyAsync(x=>x.ProductId==id&&x.OldSlug==old));
        }
    }
    private static CatalogAdminService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db)=>new(db,new EditProductRequestValidator(),new EditCategoryRequestValidator());
}
