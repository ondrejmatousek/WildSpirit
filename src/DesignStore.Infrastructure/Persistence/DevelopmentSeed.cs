using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DesignStore.Infrastructure.Persistence;

public sealed class DevelopmentSeed(ApplicationDbContext db, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles, IConfiguration configuration,
    ILogger<DevelopmentSeed> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        await SeedRolesAsync();
        await SeedAdminAsync();
        if (!await db.Categories.AnyAsync(ct)) await SeedCatalogAsync(ct);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in new[] { "Administrator", "CatalogEditor", "Warehouse", "Support" })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));
    }

    private async Task SeedAdminAsync()
    {
        var email = configuration["DevelopmentAdmin:Email"];
        var password = configuration["DevelopmentAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Development admin was not created. Configure DevelopmentAdmin secrets when needed.");
            return;
        }
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(user, "Administrator")) await users.AddToRoleAsync(user, "Administrator");
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
            DisplayOrder = i, ImageUrl = c.Item7,
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
                Category = categories[ci], Sku = $"WS-{i + 1:000}", BasePrice = 1890m + i * 1370m,
                StartingPrice = status == AvailabilityStatus.MadeToOrder ? 1890m + i * 1370m : null,
                AvailabilityStatus = status, StockQuantity = status == AvailabilityStatus.InStock ? 2 + i % 4 : 0,
                CanBeMadeToOrder = status == AvailabilityStatus.MadeToOrder, AutomaticallySwitchToMadeToOrder = i % 3 == 0,
                EstimatedProductionDays = status == AvailabilityStatus.MadeToOrder ? 35 : null,
                IsFeatured = i < 8, IsNew = i is >= 8 and < 14, IsLimited = ci == 6 || i is 18 or 19,
                DisplayOrder = i, MainImageUrl = categories[ci].ImageUrl,
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

    private static CategoryTranslation Ct(string culture, string name, string slug) => new()
    { Culture = culture, Name = name, Slug = slug, ShortDescription = "Výběr autorských výrobků pro současný interiér." };

    private static ProductTranslation Pt(string culture, string name, string slug, string shortDescription) => new()
    {
        Culture = culture, Name = name, Slug = slug, ShortDescription = shortDescription,
        LongDescription = shortDescription + " Každý kus prochází rukama našich řemeslníků a vzniká s respektem k přirozené kresbě materiálu.",
        Story = "Navrženo v našem moravském ateliéru jako tichý, dlouhověký společník každodenního života.",
        Materials = "Pečlivě vybrané přírodní materiály z odpovědných zdrojů.", Dimensions = "Rozměry a technické podrobnosti sdělíme u konkrétní varianty.",
        CareInstructions = "Ošetřujte jemně, bez agresivních čisticích prostředků.", ShippingInformation = "Bezpečné doručení po České republice a Evropě."
    };

    private static string Slug(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        return string.Concat(normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Replace(' ', '-').Replace("ý", "y").Replace("ů", "u");
    }
}
