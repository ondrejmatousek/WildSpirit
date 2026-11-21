using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.Domain.Marketing;
using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DesignStore.Infrastructure.Persistence;

public sealed class DevelopmentSeed(ApplicationDbContext db, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles, IConfiguration configuration)
{
    public static readonly string[] Roles = ["Administrator", "ContentManager", "CatalogEditor", "Warehouse", "Support", "Customer"];

    public async Task RunAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        await SeedRolesAsync();
        await SeedAdminAsync();
        if (!await db.Categories.AnyAsync(ct)) await SeedCatalogAsync(ct);
        if (!await db.Materials.AnyAsync(ct)) await SeedStage2CatalogAsync(ct);
        if (!await db.DiscountCodes.AnyAsync(ct)) { db.DiscountCodes.Add(new DiscountCode { Code = "WELCOME10", Type = DiscountType.Percentage, Value = 10, MinimumOrderValue = 1000, IsActive = true }); await db.SaveChangesAsync(ct); }
        await SeedGiftVoucherAsync(ct);
        if (!await db.ContentPages.AnyAsync(ct)) await SeedMarketingAsync(ct);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));
    }

    private async Task SeedAdminAsync()
    {
        var email = configuration["DevelopmentAdmin:Email"] ?? "admin@wildspirit.local";
        var password = configuration["DevelopmentAdmin:Password"] ?? "WildSpirit.Dev.Admin.2026!";
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var update = await users.UpdateAsync(user);
            if (!update.Succeeded) throw new InvalidOperationException(string.Join("; ", update.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(user, "Administrator")) await users.AddToRoleAsync(user, "Administrator");

        var customerEmail = configuration["DevelopmentCustomer:Email"] ?? "customer@wildspirit.local";
        var customerPassword = configuration["DevelopmentCustomer:Password"] ?? "WildSpirit.Dev.Customer.2026!";
        var customer = await users.FindByEmailAsync(customerEmail);
        if (customer is null)
        {
            customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, EmailConfirmed = true };
            var result = await users.CreateAsync(customer, customerPassword);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!customer.EmailConfirmed)
        {
            customer.EmailConfirmed = true;
            var update = await users.UpdateAsync(customer);
            if (!update.Succeeded) throw new InvalidOperationException(string.Join("; ", update.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(customer, "Customer")) await users.AddToRoleAsync(customer, "Customer");
    }

    private async Task SeedCatalogAsync(CancellationToken ct)
    {
        var categoryData = new[]
        {
            ("Nábytek", "Furniture", "Möbel", "nabytek", "furniture", "mobel", "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80"),
            ("Světla", "Lighting", "Leuchten", "svetla", "lighting", "leuchten", "https://images.unsplash.com/photo-1540932239986-30128078f3c5?auto=format&fit=crop&w=1200&q=80"),
            ("Kožené kabelky", "Leather bags", "Ledertaschen", "kozene-kabelky", "leather-bags", "ledertaschen", "https://images.unsplash.com/photo-1584917865442-de89df76afd3?auto=format&fit=crop&w=1200&q=80"),
            ("Kožené pásky", "Leather belts", "Ledergürtel", "kozene-pasky", "leather-belts", "ledergurtel", "https://images.unsplash.com/photo-1624222247344-550fb60583dc?auto=format&fit=crop&w=1200&q=80"),
            ("Kožené doplňky", "Leather accessories", "Lederaccessoires", "kozene-doplnky", "leather-accessories", "lederaccessoires", "https://images.unsplash.com/photo-1606503825008-909a67e63c3d?auto=format&fit=crop&w=1200&q=80"),
            ("Bytové doplňky", "Home accessories", "Wohnaccessoires", "bytove-doplnky", "home-accessories", "wohnaccessoires", "https://images.unsplash.com/photo-1618221195710-dd6b41faaea6?auto=format&fit=crop&w=1200&q=80"),
            ("Limitované kolekce", "Limited collections", "Limitierte Kollektionen", "limitovane-kolekce", "limited-collections", "limitierte-kollektionen", "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?auto=format&fit=crop&w=1200&q=80")
        };
        var categories = categoryData.Select((c, i) => new Category
        {
            DisplayOrder = i,
            ImageUrl = c.Item7,
            Translations =
            [
                Ct("cs-CZ", c.Item1, c.Item4), Ct("en", c.Item2, c.Item5), Ct("de", c.Item3, c.Item6)
            ]
        }).ToArray();
        db.Categories.AddRange(categories);

        var names = new[]
        {
            ("Dubový jídelní stůl Linea",0),("Konferenční stolek Nero",0),("Konzolový stolek Forma",0),
            ("Křeslo Solis",0),("Lavice Terra",0),("Závěsné světlo Orbit",1),("Nástěnné světlo Frame",1),
            ("Stojací lampa Arc",1),("Stolní lampa Halo",1),("Kožená kabelka Aurora",2),
            ("Kožená kabelka Nocturne",2),("Kožená kabelka Alba",2),("Kožený pásek Heritage",3),
            ("Kožený pásek Minimal",3),("Kožený pásek Atelier",3),("Kožené pouzdro Atelier",4),
            ("Peněženka Fold",4),("Mísa Origin",5),("Svícen Brass",5),("Limitovaný objekt Monolith",6)
        };
        for (var i = 0; i < names.Length; i++)
        {
            var (name, ci) = names[i];
            var slug = Slug(name);
            var status = (i % 7) switch { 4 => AvailabilityStatus.MadeToOrder, 5 => AvailabilityStatus.Unavailable, 6 => AvailabilityStatus.Discontinued, _ => AvailabilityStatus.InStock };
            var product = new Product
            {
                Category = categories[ci],
                Sku = $"WS-{i + 1:000}",
                BasePrice = 1890m + i * 1370m,
                StartingPrice = status == AvailabilityStatus.MadeToOrder ? 1890m + i * 1370m : null,
                AvailabilityStatus = status,
                StockQuantity = status == AvailabilityStatus.InStock ? 2 + i % 4 : 0,
                CanBeMadeToOrder = status == AvailabilityStatus.MadeToOrder,
                AutomaticallySwitchToMadeToOrder = i % 3 == 0,
                EstimatedProductionDays = status == AvailabilityStatus.MadeToOrder ? 35 : null,
                IsFeatured = i < 8,
                IsNew = i is >= 8 and < 14,
                IsLimited = ci == 6 || i is 18 or 19,
                DisplayOrder = i,
                MainImageUrl = categories[ci].ImageUrl,
                Translations =
                [
                    Pt("cs-CZ", name, slug, "Autorský objekt vytvořený v malé sérii s důrazem na materiál a detail."),
                    Pt("en", name, slug + "-en", "An original object made in a small series with attention to material and detail."),
                    Pt("de", name, slug + "-de", "Ein Entwurfsobjekt in Kleinserie mit Fokus auf Material und Detail.")
                ]
            };
            db.Products.Add(product);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedStage2CatalogAsync(CancellationToken ct)
    {
        var oak = new Material { Code = "OAK", Translations = [new MaterialTranslation { Culture = "cs-CZ", Name = "Dub" }, new MaterialTranslation { Culture = "en", Name = "Oak" }, new MaterialTranslation { Culture = "de", Name = "Eiche" }] };
        var leather = new Material { Code = "LEATHER", Translations = [new MaterialTranslation { Culture = "cs-CZ", Name = "Třísločiněná kůže" }, new MaterialTranslation { Culture = "en", Name = "Vegetable-tanned leather" }, new MaterialTranslation { Culture = "de", Name = "Pflanzlich gegerbtes Leder" }] };
        var brass = new Material { Code = "BRASS", Translations = [new MaterialTranslation { Culture = "cs-CZ", Name = "Mosaz" }, new MaterialTranslation { Culture = "en", Name = "Brass" }, new MaterialTranslation { Culture = "de", Name = "Messing" }] };
        db.Materials.AddRange(oak, leather, brass);
        db.Colors.AddRange(new Color { Code = "NATURAL", Hex = "#B58B5A", Translations = [new ColorTranslation { Culture = "cs-CZ", Name = "Přírodní" }] }, new Color { Code = "BLACK", Hex = "#171717", Translations = [new ColorTranslation { Culture = "cs-CZ", Name = "Černá" }] });
        var atelier = new Collection { IsPublished = true, Translations = [new CollectionTranslation { Culture = "cs-CZ", Name = "Atelier 01", Slug = "atelier-01", Description = "První autorská kolekce." }] }; db.Collections.Add(atelier);
        var product = await db.Products.Include(x => x.Translations).Include(x => x.Variants).Include(x => x.Options).ThenInclude(x => x.Translations).Include(x => x.Options).ThenInclude(x => x.Values).ThenInclude(x => x.Translations).OrderBy(x => x.DisplayOrder).FirstAsync(ct);
        product.Collection = atelier; product.HasVariants = true; product.StockQuantity = 0;
        var variants = new[]
        {
            new ProductVariant{Product=product,Sku=$"{product.Sku}-OAK",Name="Dub přírodní",Price=product.BasePrice,AvailabilityStatus=AvailabilityStatus.InStock,StockQuantity=2},
            new ProductVariant{Product=product,Sku=$"{product.Sku}-DARK",Name="Dub tmavý",Price=product.BasePrice+2500,AvailabilityStatus=AvailabilityStatus.MadeToOrder,CanBeMadeToOrder=true,EstimatedProductionDays=35}
        };
        var productMaterial = new ProductMaterial { Product = product, Material = oak };
        var option = new ProductOption { Product = product, Type = ProductOptionType.Select, IsRequired = true, Translations = [new ProductOptionTranslation { Culture = "cs-CZ", Name = "Povrchová úprava" }], Values = [new ProductOptionValue { PriceAdjustment = 0, Translations = [new ProductOptionValueTranslation { Culture = "cs-CZ", Name = "Přírodní olej" }] }, new ProductOptionValue { PriceAdjustment = 1800, AdditionalProductionDays = 5, Translations = [new ProductOptionValueTranslation { Culture = "cs-CZ", Name = "Tmavý olej" }] }] };
        db.ProductVariants.AddRange(variants);
        db.Add(productMaterial);
        db.ProductOptions.Add(option);
        await db.SaveChangesAsync(ct);
    }


    private async Task SeedGiftVoucherAsync(CancellationToken ct)
    {
        const string productSku = "WS-GIFT-VOUCHER";
        const string categorySlug = "darkove-poukazy";

        var category = await db.Categories
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Translations.Any(t => t.Culture == "cs-CZ" && t.Slug == categorySlug), ct);

        if (category is null)
        {
            category = new Category
            {
                DisplayOrder = 950,
                IsPublished = true,
                ImageUrl = "/images/brand/gift-voucher-card.svg",
                BannerUrl = "/images/brand/gift-voucher-card.svg",
                Translations =
                [
                    GiftCategoryTranslation("cs-CZ", "Dárkové poukazy", categorySlug, "Darujte výběr z ateliéru Wild Spirit."),
                    GiftCategoryTranslation("en", "Gift vouchers", "gift-vouchers", "Give someone the freedom to choose from Wild Spirit."),
                    GiftCategoryTranslation("de", "Geschenkgutscheine", "geschenkgutscheine", "Verschenken Sie die freie Auswahl aus dem Wild-Spirit-Atelier.")
                ]
            };
            db.Categories.Add(category);
        }
        else
        {
            category.IsPublished = true;
            category.ImageUrl = string.IsNullOrWhiteSpace(category.ImageUrl) ? "/images/brand/gift-voucher-card.svg" : category.ImageUrl;
            category.BannerUrl = string.IsNullOrWhiteSpace(category.BannerUrl) ? "/images/brand/gift-voucher-card.svg" : category.BannerUrl;
            UpsertCategoryTranslation(category, "cs-CZ", "Dárkové poukazy", categorySlug, "Darujte výběr z ateliéru Wild Spirit.");
            UpsertCategoryTranslation(category, "en", "Gift vouchers", "gift-vouchers", "Give someone the freedom to choose from Wild Spirit.");
            UpsertCategoryTranslation(category, "de", "Geschenkgutscheine", "geschenkgutscheine", "Verschenken Sie die freie Auswahl aus dem Wild-Spirit-Atelier.");
        }

        var product = await db.Products
            .Include(x => x.Translations)
            .Include(x => x.Variants)
            .ThenInclude(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Sku == productSku, ct);

        if (product is null)
        {
            product = new Product { Sku = productSku };
            db.Products.Add(product);
        }

        product.Category = category;
        product.BasePrice = 1000m;
        product.StartingPrice = 1000m;
        product.VatRate = 0m;
        product.AvailabilityStatus = AvailabilityStatus.InStock;
        product.TrackInventory = false;
        product.StockQuantity = 0;
        product.CanBeMadeToOrder = false;
        product.AutomaticallySwitchToMadeToOrder = false;
        product.EstimatedProductionDays = null;
        product.IsPublished = true;
        product.IsFeatured = false;
        product.IsNew = false;
        product.IsLimited = false;
        product.HasVariants = true;
        product.DisplayOrder = 960;
        product.MainImageUrl = "/images/brand/gift-voucher-card.svg";

        UpsertProductTranslation(product, "cs-CZ", "Dárkový poukaz Wild Spirit", "darkovy-poukaz", "Darujte možnost vybrat si ručně vyráběný objekt, doplněk nebo zakázkovou práci z ateliéru Wild Spirit.");
        UpsertProductTranslation(product, "en", "Wild Spirit gift voucher", "gift-voucher", "Give someone the freedom to choose a handmade object, accessory or bespoke piece from Wild Spirit.");
        UpsertProductTranslation(product, "de", "Wild Spirit Geschenkgutschein", "geschenkgutschein", "Verschenken Sie die freie Wahl für ein handgefertigtes Objekt, Accessoire oder Einzelstück von Wild Spirit.");

        foreach (var amount in new[] { 1000m, 2500m, 5000m, 10000m })
        {
            var sku = $"{productSku}-{amount:0}";
            var variant = product.Variants.FirstOrDefault(x => x.Sku == sku);
            if (variant is null)
            {
                variant = new ProductVariant { Product = product, Sku = sku };
                product.Variants.Add(variant);
            }

            variant.Name = $"Poukaz {amount:N0} Kč";
            variant.Price = amount;
            variant.AvailabilityStatus = AvailabilityStatus.InStock;
            variant.StockQuantity = 100000;
            variant.CanBeMadeToOrder = false;
            variant.EstimatedProductionDays = null;
            UpsertVariantTranslation(variant, "cs-CZ", $"Poukaz {amount:N0} Kč");
            UpsertVariantTranslation(variant, "en", $"Voucher {amount:N0} CZK");
            UpsertVariantTranslation(variant, "de", $"Gutschein {amount:N0} CZK");
        }

        await db.SaveChangesAsync(ct);
    }
    private async Task SeedMarketingAsync(CancellationToken ct)
    {
        db.ContentPages.AddRange(
            new ContentPage { Type = ContentPageType.Story, IsPublished = true, DisplayOrder = 1, Translations = [new ContentPageTranslation { Culture = "cs-CZ", Title = "Příběh Wild Spirit", Slug = "pribeh", Summary = "Ateliér, materiály a objekty s charakterem.", BodyHtml = "<p>Wild Spirit vzniká z respektu k dřevu, železu, kameni a poctivé ruční práci.</p>", SeoTitle = "Příběh Wild Spirit", SeoDescription = "Autorský design, řemeslo a přírodní materiály." }] },
            new ContentPage { Type = ContentPageType.Craft, IsPublished = true, DisplayOrder = 2, Translations = [new ContentPageTranslation { Culture = "cs-CZ", Title = "Řemeslo", Slug = "remeslo", Summary = "Proces výroby a péče o detail.", BodyHtml = "<p>Každý kus prochází návrhem, výrobou, kontrolou a finální péčí v ateliéru.</p>", SeoTitle = "Řemeslo Wild Spirit", SeoDescription = "Jak vznikají objekty Wild Spirit." }] });
        db.GalleryItems.AddRange(
            new GalleryItem { IsPublished = true, DisplayOrder = 1, ImageUrl = "/images/hero-meadow-sunset.png", Translations = [new GalleryItemTranslation { Culture = "cs-CZ", Title = "Louka při západu", Description = "Sepiová atmosféra ateliéru a přírodních materiálů." }] },
            new GalleryItem { IsPublished = true, DisplayOrder = 2, ImageUrl = "/images/hero-meadow-sunset.png", Translations = [new GalleryItemTranslation { Culture = "cs-CZ", Title = "Dřevo, železo, kámen", Description = "Materiály, které přirozeně stárnou a získávají charakter." }] });
        db.Reviews.AddRange(
            new Review { IsPublished = true, Culture = "cs-CZ", AuthorName = "Ateliérový zákazník", Rating = 5, Text = "Kus, který má vlastní ticho a váhu. Přesně to jsme hledali." },
            new Review { IsPublished = true, Culture = "cs-CZ", AuthorName = "Interiérový architekt", Rating = 5, Text = "Silná materiálová estetika bez prázdného efektu." });
        await db.SaveChangesAsync(ct);
    }


    private static CategoryTranslation GiftCategoryTranslation(string culture, string name, string slug, string shortDescription) => new()
    {
        Culture = culture,
        Name = name,
        Slug = slug,
        ShortDescription = shortDescription,
        LongDescription = shortDescription,
        SeoTitle = name,
        SeoDescription = shortDescription
    };

    private static void UpsertCategoryTranslation(Category category, string culture, string name, string slug, string shortDescription)
    {
        var translation = category.Translations.FirstOrDefault(x => x.Culture == culture);
        if (translation is null)
        {
            translation = new CategoryTranslation { Culture = culture };
            category.Translations.Add(translation);
        }
        translation.Name = name;
        translation.Slug = slug;
        translation.ShortDescription = shortDescription;
        translation.LongDescription = shortDescription;
        translation.SeoTitle = name;
        translation.SeoDescription = shortDescription;
    }

    private static void UpsertProductTranslation(Product product, string culture, string name, string slug, string shortDescription)
    {
        var translation = product.Translations.FirstOrDefault(x => x.Culture == culture);
        if (translation is null)
        {
            translation = new ProductTranslation { Culture = culture };
            product.Translations.Add(translation);
        }
        translation.Name = name;
        translation.Slug = slug;
        translation.ShortDescription = shortDescription;
        translation.LongDescription = shortDescription + " Poukaz po objednání připravíme a zašleme e-mailem. Hodí se pro produkty skladem i pro individuální domluvu zakázkové výroby.";
        translation.Story = "Malý rituál darování pro někoho, kdo má rád poctivé materiály, tiché objekty a věci s charakterem.";
        translation.Materials = "Elektronický poukaz s kódem, připravený k předání nebo přeposlání.";
        translation.Dimensions = "Hodnotu poukazu vyberete před vložením do košíku.";
        translation.CareInstructions = "Poukaz uchovejte tak, aby byl jeho kód čitelný. Uplatnění řešíme osobně s ateliérem.";
        translation.ShippingInformation = "Poukaz zasíláme e-mailem po zpracování objednávky.";
        translation.SeoTitle = name;
        translation.SeoDescription = shortDescription;
    }

    private static void UpsertVariantTranslation(ProductVariant variant, string culture, string name)
    {
        var translation = variant.Translations.FirstOrDefault(x => x.Culture == culture);
        if (translation is null)
        {
            translation = new ProductVariantTranslation { Culture = culture };
            variant.Translations.Add(translation);
        }
        translation.Name = name;
        translation.Description = "Hodnota dárkového poukazu.";
    }
    private static CategoryTranslation Ct(string culture, string name, string slug) => new()
    { Culture = culture, Name = name, Slug = slug, ShortDescription = "Výběr autorských výrobků pro současný interiér." };

    private static ProductTranslation Pt(string culture, string name, string slug, string shortDescription) => new()
    {
        Culture = culture,
        Name = name,
        Slug = slug,
        ShortDescription = shortDescription,
        LongDescription = shortDescription + " Každý kus prochází rukama našich řemeslníků a vzniká s respektem k přirozené kresbě materiálu.",
        Story = "Navrženo v našem moravském ateliéru jako tichý, dlouhověký společník každodenního života.",
        Materials = "Pečlivě vybrané přírodní materiály z odpovědných zdrojů.",
        Dimensions = "Rozměry a technické podrobnosti sdělíme u konkrétní varianty.",
        CareInstructions = "Ošetřujte jemně, bez agresivních čisticích prostředků.",
        ShippingInformation = "Bezpečné doručení po České republice a Evropě."
    };

    private static string Slug(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        return string.Concat(normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Replace(' ', '-').Replace("ý", "y").Replace("ů", "u");
    }
}
