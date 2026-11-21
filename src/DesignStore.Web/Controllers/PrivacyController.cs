using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Identity;
using DesignStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DesignStore.Web.Controllers;

public sealed class PrivacyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private const string ConsentCookieName = "wildspirit_cookie_consent";

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult CookieConsent(string? returnUrl = null)
    {
        Response.Cookies.Append(ConsentCookieName, "necessary", new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : $"/{CurrentCulture()}");
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var userId = UserGuid();
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var normalizedEmail = user.NormalizedEmail ?? string.Empty;
        var export = new
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Account = new
            {
                user.Id,
                user.Email,
                user.PhoneNumber,
                user.PreferredCulture,
                user.PreferredCurrency,
                user.EmailConfirmed,
                user.LockoutEnabled
            },
            Addresses = await db.CustomerAddresses.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new { x.Label, x.FullName, x.Company, x.Street, x.City, x.PostalCode, x.CountryCode, x.Phone, x.IsDefaultBilling, x.IsDefaultShipping, x.CreatedAtUtc, x.UpdatedAtUtc })
                .ToListAsync(ct),
            Orders = await db.Orders.AsNoTracking()
                .Where(x => x.UserId == userId || x.Email.ToLower() == (user.Email ?? "").ToLower())
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Number, x.Email, x.Status, x.PaymentStatus, x.Currency, x.Subtotal, x.DiscountTotal, x.ShippingTotal, x.TaxTotal, x.Total, x.ShippingMethod, x.CreatedAtUtc })
                .ToListAsync(ct),
            Inquiries = await db.ProductInquiries.AsNoTracking()
                .Where(x => x.Email.ToLower() == (user.Email ?? "").ToLower())
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Number, x.Status, x.Email, x.FullName, x.CountryCode, x.Quantity, x.RequestedDeadline, x.Note, x.ProductName, x.VariantName, x.Sku, x.CreatedAtUtc })
                .ToListAsync(ct),
            Production = await db.ProductionJobs.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Number, x.ProductNameSnapshot, x.SkuSnapshot, x.Quantity, x.Status, x.PlannedStartUtc, x.PlannedCompletionUtc, x.ActualCompletionUtc, x.CreatedAtUtc })
                .ToListAsync(ct),
            Favorites = await db.FavoriteProducts.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new { x.ProductId, x.CreatedAtUtc })
                .ToListAsync(ct),
            Newsletter = await db.NewsletterSubscriptions.AsNoTracking()
                .Where(x => x.NormalizedEmail == normalizedEmail)
                .Select(x => new { x.Email, x.Culture, x.Status, x.ConfirmedAtUtc, x.UnsubscribedAtUtc, x.CreatedAtUtc })
                .ToListAsync(ct),
            ContactMessages = await db.ContactMessages.AsNoTracking()
                .Where(x => x.Email.ToLower() == (user.Email ?? "").ToLower())
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Email, x.FullName, x.Subject, x.Message, x.Culture, x.Status, x.CreatedAtUtc })
                .ToListAsync(ct)
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"wildspirit-data-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDeletion(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = "PrivacyDeletionRequested",
            Payload = JsonSerializer.Serialize(new
            {
                user.Id,
                user.Email,
                RequestedAtUtc = DateTime.UtcNow,
                Note = "Manual review required before anonymization because invoices, orders and legal records may have retention obligations."
            }),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        TempData["Success"] = "Žádost o zrušení účtu byla přijata. Osobní údaje projdou ruční kontrolou kvůli zákonným retenčním povinnostem.";
        return RedirectToAction("Index", "Account", new { culture = CurrentCulture() });
    }

    private Guid UserGuid() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentCulture() => RouteData.Values["culture"]?.ToString() ?? "cs-CZ";
}
