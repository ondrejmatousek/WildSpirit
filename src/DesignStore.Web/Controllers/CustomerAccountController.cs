using DesignStore.Application.Production;
using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace DesignStore.Web.Controllers;

[Authorize, Route("{culture}/Account")]
public sealed class CustomerAccountController(ApplicationDbContext db, IProductionService production) : Controller
{
    [HttpGet("Orders")] public async Task<IActionResult> Orders(CancellationToken ct) => Ok(await db.Orders.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Number, x.Status, x.Total, x.CreatedAtUtc }).ToListAsync(ct));
    [HttpGet("Inquiries")] public async Task<IActionResult> Inquiries(CancellationToken ct) => Ok(await db.ProductInquiries.AsNoTracking().Where(x => x.Email == UserEmail).OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Number, x.Status, x.ProductName, x.CreatedAtUtc }).ToListAsync(ct));
    [HttpGet("Quotes")] public async Task<IActionResult> Quotes(CancellationToken ct) => Ok(await db.Quotes.AsNoTracking().Where(x => x.ProductInquiry.Email == UserEmail).OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Number, x.Status, x.Total, x.ValidUntilUtc }).ToListAsync(ct));
    [HttpGet("Production")] public async Task<IActionResult> Production(CancellationToken ct) => Ok(await production.GetForCustomerAsync(UserId, ct));
    [HttpGet("Production/{id:guid}")] public async Task<IActionResult> ProductionDetail(Guid id, CancellationToken ct) { var job = await production.GetCustomerJobAsync(UserId, id, ct); return job is null ? NotFound() : Ok(job); }
    [HttpGet("Addresses")] public async Task<IActionResult> Addresses(CancellationToken ct) => Ok(await db.CustomerAddresses.AsNoTracking().Where(x => x.UserId == UserId).OrderBy(x => x.Label).ToListAsync(ct));
    [HttpPost("Addresses"), ValidateAntiForgeryToken] public async Task<IActionResult> AddAddress(CustomerAddress model, CancellationToken ct) { if (!ModelState.IsValid) return ValidationProblem(ModelState); model.Id = Guid.NewGuid(); model.UserId = UserId; db.CustomerAddresses.Add(model); await db.SaveChangesAsync(ct); return Ok(model.Id); }
    [HttpGet("Favorites")] public async Task<IActionResult> Favorites(CancellationToken ct) => Ok(await db.FavoriteProducts.AsNoTracking().Where(x => x.UserId == UserId).Join(db.Products, f => f.ProductId, p => p.Id, (f, p) => new { p.Id, p.Sku, p.BasePrice }).ToListAsync(ct));
    [HttpPost("Favorites/{productId:guid}"), ValidateAntiForgeryToken] public async Task<IActionResult> ToggleFavorite(Guid productId, CancellationToken ct) { var f = await db.FavoriteProducts.SingleOrDefaultAsync(x => x.UserId == UserId && x.ProductId == productId, ct); if (f is null) { if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsPublished, ct)) return NotFound(); db.FavoriteProducts.Add(new FavoriteProduct { UserId = UserId, ProductId = productId }); } else db.FavoriteProducts.Remove(f); await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Favorites), new { culture }); }
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); private string UserEmail => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? ""; private string culture => RouteData.Values["culture"]?.ToString() ?? "cs-CZ";
}
