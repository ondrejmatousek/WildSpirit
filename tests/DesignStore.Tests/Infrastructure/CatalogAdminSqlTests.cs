using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Catalog;
using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace DesignStore.Tests.Infrastructure;

[Collection("SqlServer database")]
public sealed class CatalogAdminSqlTests(SqlServerDatabaseFixture database)
{
    [Fact]
    public async Task Changing_slug_creates_redirect_history()
    {
        Guid id; string old;
        await using (var db = database.CreateDb())
        {
            var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Test", Slug = $"category-{Guid.NewGuid():N}", ShortDescription = "Test" }] }; db.Categories.Add(category); await db.SaveChangesAsync();
            var service = Service(db); var create = new EditProductRequest { Sku = $"SQL-{Guid.NewGuid():N}", CategoryId = category.Id, BasePrice = 500, Translations = [new ProductTranslationInput { Culture = "cs-CZ", Name = "Produkt", Slug = $"old-{Guid.NewGuid():N}" }] }; id = await service.SaveProductAsync(create, "test", default); old = create.Translations.Single().Slug;
        }
        await using (var db = database.CreateDb())
        {
            var service = Service(db); var edit = await service.GetProductAsync(id, default); edit!.Translations.Single().Slug = $"new-{Guid.NewGuid():N}"; await service.SaveProductAsync(edit, "test", default); Assert.True(await db.ProductSlugHistories.AnyAsync(x => x.ProductId == id && x.OldSlug == old));
        }
    }
    [Fact]
    public async Task Product_editor_lookups_are_sql_translatable()
    {
        await using var db = database.CreateDb();
        db.Collections.Add(new Collection { IsPublished = true, Translations = [new CollectionTranslation { Culture = "cs-CZ", Name = "Atelier SQL", Slug = $"atelier-{Guid.NewGuid():N}" }] });
        db.Materials.Add(new Material { Code = $"MAT{Guid.NewGuid():N}"[..12].ToUpperInvariant(), Translations = [new MaterialTranslation { Culture = "cs-CZ", Name = "Dub SQL" }] });
        db.Colors.Add(new Color { Code = $"COL{Guid.NewGuid():N}"[..12].ToUpperInvariant(), Hex = "#111111", Translations = [new ColorTranslation { Culture = "cs-CZ", Name = "Cerna SQL" }] });
        await db.SaveChangesAsync();

        var service = Service(db);

        Assert.Contains(await service.GetCollectionsAsync(default), x => x.Name == "Atelier SQL");
        Assert.Contains(await service.GetMaterialsAsync(default), x => x.Name == "Dub SQL");
        Assert.Contains(await service.GetColorsAsync(default), x => x.Name == "Cerna SQL");
    }
    [Fact]
    public async Task Category_admin_crud_saves_reads_and_updates_translations_and_images()
    {
        var slugSuffix = Guid.NewGuid().ToString("N");
        Guid categoryId;
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            categoryId = await service.SaveCategoryAsync(new EditCategoryRequest
            {
                DisplayOrder = 12,
                IsPublished = true,
                ImageUrl = "/media/category-image/categories/test/category.webp",
                BannerUrl = "/media/category-image/categories/test/banner.webp",
                Translations =
                [
                    new CategoryTranslationInput{Culture="cs-CZ",Name=" Kožené lampy ",Slug="",ShortDescription=" Krátký popis ",LongDescription=" Dlouhý popis ",SeoTitle=" Kožené lampy WildSpirit ",SeoDescription=" Autorské kožené lampy. "},
                    new CategoryTranslationInput{Culture="en",Name=" Leather lamps ",Slug=$"Leather Lamps {slugSuffix}",ShortDescription="Short",LongDescription="Long",SeoTitle="Leather lamps",SeoDescription="Leather lamp category"},
                    new CategoryTranslationInput{Culture="de",Name=" Lederlampen ",Slug=$"Leder Lampen {slugSuffix}",ShortDescription="Kurz",LongDescription="Lang"}
                ]
            }, "test", default);
        }

        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            var loaded = await service.GetCategoryAsync(categoryId, default);

            Assert.NotNull(loaded);
            Assert.Equal(12, loaded.DisplayOrder);
            Assert.True(loaded.IsPublished);
            Assert.Equal("/media/category-image/categories/test/category.webp", loaded.ImageUrl);
            Assert.Equal("/media/category-image/categories/test/banner.webp", loaded.BannerUrl);
            Assert.Equal("kozene-lampy", loaded.Translations.Single(x => x.Culture == "cs-CZ").Slug);
            Assert.Equal($"leather-lamps-{slugSuffix}", loaded.Translations.Single(x => x.Culture == "en").Slug);
            Assert.Equal($"leder-lampen-{slugSuffix}", loaded.Translations.Single(x => x.Culture == "de").Slug);

            loaded.DisplayOrder = 3;
            loaded.IsPublished = false;
            loaded.ImageUrl = "/media/category-image/categories/test/category-edited.webp";
            loaded.BannerUrl = "/media/category-image/categories/test/banner-edited.webp";
            loaded.Translations.Single(x => x.Culture == "cs-CZ").Name = "Kožené lampy edit";
            loaded.Translations.Single(x => x.Culture == "cs-CZ").Slug = "";
            foreach (var optional in loaded.Translations.Where(x => x.Culture != "cs-CZ"))
            {
                optional.Name = "";
                optional.Slug = "";
                optional.ShortDescription = "";
                optional.LongDescription = "";
                optional.SeoTitle = "";
                optional.SeoDescription = "";
            }
            await service.SaveCategoryAsync(loaded, "test", default);
        }

        await using (var db = database.CreateDb())
        {
            var updated = await Service(db).GetCategoryAsync(categoryId, default);

            Assert.NotNull(updated);
            Assert.Equal(3, updated.DisplayOrder);
            Assert.False(updated.IsPublished);
            Assert.Equal("/media/category-image/categories/test/category-edited.webp", updated.ImageUrl);
            Assert.Equal("/media/category-image/categories/test/banner-edited.webp", updated.BannerUrl);
            Assert.Single(updated.Translations);
            Assert.Equal("kozene-lampy-edit", updated.Translations.Single().Slug);
        }
    }
    [Fact]
    public async Task Category_create_ignores_empty_optional_translation_tabs_and_normalizes_slug()
    {
        await using var db = database.CreateDb();
        var service = Service(db);
        var id = await service.SaveCategoryAsync(new EditCategoryRequest
        {
            Translations =
            [
                new CategoryTranslationInput{Culture=" cs-CZ ",Name=$" Kožené lampy Aurora {Guid.NewGuid():N} ",Slug="",ShortDescription="  "},
                new CategoryTranslationInput{Culture="en",Name="",Slug="",ShortDescription="  "},
                new CategoryTranslationInput{Culture="de"}
            ]
        }, "test", default);

        var translations = await db.CategoryTranslations.AsNoTracking().Where(x => x.CategoryId == id).ToListAsync();

        Assert.Single(translations);
        Assert.Equal("cs-CZ", translations.Single().Culture);
        Assert.StartsWith("kozene-lampy-aurora-", translations.Single().Slug);
    }
    [Fact]
    public async Task Category_duplicate_slug_is_reported_before_database_unique_index_failure()
    {
        var slug = $"duplicate-category-{Guid.NewGuid():N}";
        await using var db = database.CreateDb();
        var service = Service(db);
        await service.SaveCategoryAsync(new EditCategoryRequest { Translations = [new CategoryTranslationInput { Culture = "cs-CZ", Name = "První", Slug = slug }] }, "test", default);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.SaveCategoryAsync(new EditCategoryRequest { Translations = [new CategoryTranslationInput { Culture = "cs-CZ", Name = "Druhá", Slug = slug }] }, "test", default));

        Assert.Contains(slug, ex.Message);
    }
    [Fact]
    public async Task Reference_admin_crud_saves_material_color_and_collection()
    {
        await using var db = database.CreateDb();
        var service = Service(db);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var materialId = await service.SaveReferenceAsync(new ReferenceItemRequest
        {
            Type = "material",
            Code = $"MAT-{suffix}",
            Name = "Dub masiv",
            NameEn = "Solid oak",
            NameDe = "Massiveiche",
            IsActive = true
        }, "test", default);
        var colorId = await service.SaveReferenceAsync(new ReferenceItemRequest
        {
            Type = "color",
            Code = $"COL-{suffix}",
            Name = "Koňaková",
            NameEn = "Cognac",
            Hex = "#9B5B2E",
            IsActive = true
        }, "test", default);
        var collectionId = await service.SaveReferenceAsync(new ReferenceItemRequest
        {
            Type = "collection",
            Code = $"COLL-{suffix}",
            Name = "Atelier rutina",
            NameEn = "Atelier routine",
            Slug = $"atelier-rutina-{suffix}",
            IsActive = true
        }, "test", default);

        await service.SaveReferenceAsync(new ReferenceItemRequest
        {
            Id = materialId,
            Type = "material",
            Code = $"MAT-{suffix}",
            Name = "Dub upravený",
            NameEn = "Edited oak",
            IsActive = false
        }, "test", default);
        await service.SaveReferenceAsync(new ReferenceItemRequest
        {
            Id = colorId,
            Type = "color",
            Code = $"COL-{suffix}",
            Name = "Koňaková edit",
            Hex = "#8A4F27",
            IsActive = false
        }, "test", default);
        await service.SaveReferenceAsync(new ReferenceItemRequest
        {
            Id = collectionId,
            Type = "collection",
            Code = $"COLL-{suffix}",
            Name = "Atelier rutina edit",
            Slug = $"atelier-rutina-edit-{suffix}",
            IsActive = false
        }, "test", default);

        var material = await db.Materials.AsNoTracking().Include(x => x.Translations).SingleAsync(x => x.Id == materialId);
        var color = await db.Colors.AsNoTracking().Include(x => x.Translations).SingleAsync(x => x.Id == colorId);
        var collection = await db.Collections.AsNoTracking().Include(x => x.Translations).SingleAsync(x => x.Id == collectionId);

        Assert.False(material.IsActive);
        Assert.Equal("Dub upravený", material.Translations.Single(x => x.Culture == "cs-CZ").Name);
        Assert.False(color.IsActive);
        Assert.Equal("#8A4F27", color.Hex);
        Assert.Equal("Koňaková edit", color.Translations.Single(x => x.Culture == "cs-CZ").Name);
        Assert.False(collection.IsPublished);
        Assert.Equal($"atelier-rutina-edit-{suffix}", collection.Translations.Single(x => x.Culture == "cs-CZ").Slug);
    }
    [Fact]
    public async Task New_product_sku_is_generated_from_existing_product_sequence()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Sku test", Slug = $"sku-category-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        db.Categories.Add(category);
        db.Products.Add(new Product { Sku = "WS-000041", Category = category, BasePrice = 100, Translations = [new ProductTranslation { Culture = "cs-CZ", Name = "Existing", Slug = $"existing-{Guid.NewGuid():N}" }] });
        await db.SaveChangesAsync();

        var service = Service(db);

        Assert.Equal("WS-000042", await service.GetNextProductSkuAsync(default));
    }
    [Fact]
    public async Task Product_admin_crud_supports_ready_and_custom_modes()
    {
        Guid productId;
        await using (var db = database.CreateDb())
        {
            var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Crud test", Slug = $"crud-category-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            var service = Service(db);
            var ready = new EditProductRequest
            {
                Sku = $"CRUD-{Guid.NewGuid():N}"[..18],
                CategoryId = category.Id,
                BasePrice = 4900,
                VatRate = 21,
                AvailabilityStatus = AvailabilityStatus.InStock,
                TrackInventory = true,
                StockQuantity = 1,
                IsPublished = true,
                Translations = [new ProductTranslationInput { Culture = "cs-CZ", Name = "Hotová kabelka", Slug = $"hotova-kabelka-{Guid.NewGuid():N}", ShortDescription = "Hotový kus", LongDescription = "Hotový kus skladem." }]
            };

            productId = await service.SaveProductAsync(ready, "test", default);
        }
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            var loaded = await service.GetProductAsync(productId, default);
            Assert.NotNull(loaded);
            Assert.Equal(4900, loaded.BasePrice);
            Assert.Equal(1, loaded.StockQuantity);
            Assert.True(loaded.TrackInventory);
            Assert.Equal(AvailabilityStatus.InStock, loaded.AvailabilityStatus);

            loaded.StartingPrice = 7900;
            loaded.AvailabilityStatus = AvailabilityStatus.MadeToOrder;
            loaded.TrackInventory = false;
            loaded.StockQuantity = 0;
            loaded.CanBeMadeToOrder = true;
            loaded.EstimatedProductionDays = 30;
            loaded.Translations.Single(x => x.Culture == "cs-CZ").Name = "Kabelka na zakázku";
            loaded.Translations.Single(x => x.Culture == "cs-CZ").Slug = $"kabelka-na-zakazku-{Guid.NewGuid():N}";
            await service.SaveProductAsync(loaded, "test", default);
        }
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            var updated = await service.GetProductAsync(productId, default);
            Assert.NotNull(updated);
            Assert.Equal(7900, updated.StartingPrice);
            Assert.Equal(AvailabilityStatus.MadeToOrder, updated.AvailabilityStatus);
            Assert.False(updated.TrackInventory);
            Assert.Equal(0, updated.StockQuantity);
            Assert.True(updated.CanBeMadeToOrder);
            Assert.Equal(30, updated.EstimatedProductionDays);
            Assert.Contains(await service.GetProductsAsync(default), x => x.Id == productId && x.Name == "Kabelka na zakázku");

            await service.ArchiveProductAsync(productId, "test", default);
        }
        await using (var db = database.CreateDb())
        {
            var product = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);
            Assert.False(product.IsPublished);
        }
    }
    [Fact]
    public async Task Product_slug_is_normalized_from_admin_input_before_validation()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Slug normalize", Slug = $"slug-category-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = Service(db);
        var request = new EditProductRequest
        {
            Sku = $"SLUG-{Guid.NewGuid():N}"[..18],
            CategoryId = category.Id,
            BasePrice = 100,
            Translations = [new ProductTranslationInput { Culture = "de", Name = "Ledertasche aus ehrlichem Leder", Slug = "Ledertasche aus ehrlichem Leder Aurora" }]
        };

        var id = await service.SaveProductAsync(request, "test", default);
        var saved = await db.ProductTranslations.AsNoTracking().SingleAsync(x => x.ProductId == id && x.Culture == "de");

        Assert.Equal("ledertasche-aus-ehrlichem-leder-aurora", saved.Slug);
    }
    [Fact]
    public async Task Product_can_be_saved_with_empty_optional_translation_tabs()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Optional translations", Slug = $"optional-category-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = Service(db);
        var request = new EditProductRequest
        {
            Sku = $"OPT-{Guid.NewGuid():N}"[..18],
            CategoryId = category.Id,
            BasePrice = 100,
            Translations =
            [
                new ProductTranslationInput{Culture="cs-CZ",Name="Kožená kabelka",Slug="Kožená kabelka Aurora"},
                new ProductTranslationInput{Culture="en"},
                new ProductTranslationInput{Culture="de"}
            ]
        };

        var id = await service.SaveProductAsync(request, "test", default);
        var translations = await db.ProductTranslations.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.Culture).ToListAsync();

        Assert.Single(translations);
        Assert.Equal("cs-CZ", translations[0].Culture);
        Assert.Equal("kozena-kabelka-aurora", translations[0].Slug);
    }
    [Fact]
    public async Task Product_create_ignores_blank_optional_nested_rows()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Blank nested", Slug = $"blank-nested-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = Service(db);
        var request = new EditProductRequest
        {
            Sku = $" blank-{Guid.NewGuid():N} "[..20],
            CategoryId = category.Id,
            BasePrice = 4999,
            VatRate = 21,
            AvailabilityStatus = AvailabilityStatus.InStock,
            TrackInventory = true,
            StockQuantity = 1,
            HasVariants = false,
            Translations =
            [
                new ProductTranslationInput{Culture=" cs-CZ ",Name=$" Kožená kabelka Aurora {Guid.NewGuid():N} ",Slug="",ShortDescription=null!,LongDescription="  "},
                new ProductTranslationInput{Culture="en",Name="",Slug="",ShortDescription="  "},
                new ProductTranslationInput{Culture="de"}
            ],
            Variants = [new ProductVariantInput { Sku = "  ", Name = "  " }],
            Options = [new ProductOptionInput { Name = "  ", Values = [new ProductOptionValueInput { Name = "  " }] }]
        };

        var id = await service.SaveProductAsync(request, "test", default);

        var product = await db.Products.AsNoTracking().SingleAsync(x => x.Id == id);
        var translations = await db.ProductTranslations.AsNoTracking().Where(x => x.ProductId == id).ToListAsync();
        Assert.StartsWith("BLANK-", product.Sku);
        Assert.Single(translations);
        Assert.StartsWith("kozena-kabelka-aurora-", translations.Single().Slug);
        Assert.False(await db.ProductVariants.AsNoTracking().AnyAsync(x => x.ProductId == id));
        Assert.False(await db.ProductOptions.AsNoTracking().AnyAsync(x => x.ProductId == id));
    }
    [Fact]
    public async Task Product_edit_can_clear_optional_translation_tabs_and_ignores_blank_new_rows()
    {
        Guid productId;
        await using (var db = database.CreateDb())
        {
            var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Edit blank", Slug = $"edit-blank-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            productId = await Service(db).SaveProductAsync(new EditProductRequest
            {
                Sku = $"EDT-{Guid.NewGuid():N}"[..18],
                CategoryId = category.Id,
                BasePrice = 100,
                Translations =
                [
                    new ProductTranslationInput{Culture="cs-CZ",Name="Produkt",Slug=$"produkt-{Guid.NewGuid():N}"},
                    new ProductTranslationInput{Culture="en",Name="Product",Slug=$"product-{Guid.NewGuid():N}"}
                ],
                Options = [new ProductOptionInput { Name = "Barva", Values = [new ProductOptionValueInput { Name = "Hnědá", IsActive = true }] }]
            }, "test", default);
        }
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            var edit = await service.GetProductAsync(productId, default);
            edit!.Translations.Single(x => x.Culture == "cs-CZ").Name = "Produkt po editaci";
            edit.Translations.Single(x => x.Culture == "cs-CZ").Slug = "";
            edit.Translations.Single(x => x.Culture == "en").Name = "";
            edit.Translations.Single(x => x.Culture == "en").Slug = "";
            edit.Translations.Add(new ProductTranslationInput { Culture = "de" });
            edit.Options.Add(new ProductOptionInput { Name = "  ", Values = [new ProductOptionValueInput { Name = "  " }] });

            await service.SaveProductAsync(edit, "test", default);
        }
        await using (var db = database.CreateDb())
        {
            var translations = await db.ProductTranslations.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync();
            var options = await db.ProductOptions.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync();

            Assert.Single(translations);
            Assert.Equal("cs-CZ", translations.Single().Culture);
            Assert.Equal("produkt-po-editaci", translations.Single().Slug);
            Assert.Single(options);
            Assert.Equal("Barva", await db.Set<ProductOptionTranslation>().AsNoTracking().Where(x => x.ProductOptionId == options.Single().Id).Select(x => x.Name).SingleAsync());
        }
    }
    [Fact]
    public async Task Variant_product_ignores_blank_variant_rows_but_keeps_valid_variant()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Variant blank", Slug = $"variant-blank-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = Service(db);
        var request = new EditProductRequest
        {
            Sku = $"VAR-{Guid.NewGuid():N}"[..18],
            CategoryId = category.Id,
            BasePrice = 0,
            VatRate = 21,
            AvailabilityStatus = AvailabilityStatus.InStock,
            HasVariants = true,
            StockQuantity = 0,
            Translations = [new ProductTranslationInput { Culture = "cs-CZ", Name = "Variantový produkt", Slug = "" }],
            Variants =
            [
                new ProductVariantInput{Sku=$"VAR-A-{Guid.NewGuid():N}"[..18],Name="Malá",Price=1200,AvailabilityStatus=AvailabilityStatus.InStock,StockQuantity=1},
                new ProductVariantInput{Sku="",Name=""}
            ]
        };

        var id = await service.SaveProductAsync(request, "test", default);
        var variants = await db.ProductVariants.AsNoTracking().Where(x => x.ProductId == id).ToListAsync();

        Assert.Single(variants);
        Assert.Equal("Malá", await db.ProductVariantTranslations.AsNoTracking().Where(x => x.ProductVariantId == variants.Single().Id && x.Culture == "cs-CZ").Select(x => x.Name).SingleAsync());
    }
    [Fact]
    public async Task Custom_made_product_can_be_created_with_ready_only_fields_empty()
    {
        await using var db = database.CreateDb();
        var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Custom blank", Slug = $"custom-blank-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = Service(db);
        var request = new EditProductRequest
        {
            Sku = $"CUS-{Guid.NewGuid():N}"[..18],
            CategoryId = category.Id,
            BasePrice = 0,
            StartingPrice = null,
            VatRate = 21,
            AvailabilityStatus = AvailabilityStatus.MadeToOrder,
            TrackInventory = false,
            StockQuantity = 0,
            CanBeMadeToOrder = true,
            EstimatedProductionDays = null,
            Translations =
            [
                new ProductTranslationInput{Culture="cs-CZ",Name="Kabelka na zakázku",Slug=""},
                new ProductTranslationInput{Culture="en"},
                new ProductTranslationInput{Culture="de"}
            ]
        };

        var id = await service.SaveProductAsync(request, "test", default);
        var saved = await db.Products.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Equal(AvailabilityStatus.MadeToOrder, saved.AvailabilityStatus);
        Assert.False(saved.TrackInventory);
        Assert.Equal(0, saved.StockQuantity);
        Assert.True(saved.CanBeMadeToOrder);
        Assert.Null(saved.StartingPrice);
    }
    [Fact]
    public async Task Adding_product_images_updates_primary_without_tracked_product_concurrency_conflict()
    {
        Guid productId;
        await using (var db = database.CreateDb())
        {
            var category = new Category { Translations = [new CategoryTranslation { Culture = "cs-CZ", Name = "Image category", Slug = $"image-category-{Guid.NewGuid():N}", ShortDescription = "Test" }] };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            productId = await Service(db).SaveProductAsync(new EditProductRequest
            {
                Sku = $"IMG-{Guid.NewGuid():N}"[..18],
                CategoryId = category.Id,
                BasePrice = 100,
                Translations = [new ProductTranslationInput { Culture = "cs-CZ", Name = "Produkt s fotkou", Slug = $"produkt-s-fotkou-{Guid.NewGuid():N}" }]
            }, "test", default);
        }
        Guid firstId;
        Guid secondId;
        await using (var db = database.CreateDb())
        {
            var service = Service(db);
            firstId = await service.AddProductImageAsync(productId, new StoredImage("products/a.webp", "products/a-thumb.webp", "image/webp", 1200, 900), "Prvni", true, "test", default);
            secondId = await service.AddProductImageAsync(productId, new StoredImage("products/b.webp", "products/b-thumb.webp", "image/webp", 1200, 900), "Druha", true, "test", default);
        }
        await using (var db = database.CreateDb())
        {
            var images = await db.ProductImages.AsNoTracking().Where(x => x.ProductId == productId).OrderBy(x => x.DisplayOrder).ToListAsync();
            var product = await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId);
            var edit = await Service(db).GetProductAsync(productId, default);

            Assert.Equal(2, images.Count);
            Assert.False(images.Single(x => x.Id == firstId).IsPrimary);
            Assert.True(images.Single(x => x.Id == secondId).IsPrimary);
            Assert.Equal($"/media/product/{secondId}", product.MainImageUrl);
            Assert.NotNull(edit);
            Assert.Equal(2, edit.Images.Count);
            Assert.Equal(secondId, edit.Images[0].Id);
            Assert.True(edit.Images[0].IsPrimary);
            Assert.Equal($"/media/product/{secondId}", edit.Images[0].Url);
            Assert.Equal($"/media/product/{secondId}?thumbnail=true", edit.Images[0].ThumbnailUrl);
        }
    }
    private static CatalogAdminService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db) => new(db, new EditProductRequestValidator(), new EditCategoryRequestValidator());
}
