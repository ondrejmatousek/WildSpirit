using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Persistence;
using DesignStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Web.Controllers;

[Route("{culture:regex(^cs-CZ|en|de$)}/gift-voucher")]
public sealed class GiftVoucherController(ApplicationDbContext db) : Controller
{
    private const string GiftVoucherSku = "WS-GIFT-VOUCHER";

    [HttpGet("")]
    public async Task<IActionResult> Index(string culture, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Sku == GiftVoucherSku && x.IsPublished, ct);

        if (product is null)
        {
            return View(new GiftVoucherPageModel(
                Guid.Empty,
                "Dárkový poukaz",
                "Dárkový poukaz bude dostupný po dokončení nastavení katalogu.",
                "/images/brand/gift-voucher-card.svg",
                []));
        }

        var fallback = culture == "cs-CZ" ? "en" : "cs-CZ";
        var translation = product.Translations.FirstOrDefault(x => x.Culture == culture)
            ?? product.Translations.FirstOrDefault(x => x.Culture == fallback)
            ?? product.Translations.First();

        var amounts = product.Variants
            .Where(x => x.AvailabilityStatus == AvailabilityStatus.InStock && x.StockQuantity > 0)
            .OrderBy(x => x.Price ?? product.BasePrice)
            .Select(x => new GiftVoucherAmountOption(x.Id, x.Name, x.Price ?? product.BasePrice))
            .ToList();

        return View(new GiftVoucherPageModel(product.Id, translation.Name, translation.ShortDescription, product.MainImageUrl, amounts));
    }
}