using DesignStore.Application.Production; using DesignStore.Domain.Commerce; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using DesignStore.Infrastructure.Persistence;
namespace DesignStore.Web.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles="Administrator,ContentManager"), Route("{culture}/Admin/Production")]
public sealed class ProductionController(ApplicationDbContext db,IProductionService service) : Controller
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>Ok(await db.ProductionJobs.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new{x.Id,x.Number,x.ProductNameSnapshot,x.Status,x.PlannedCompletionUtc}).ToListAsync(ct));
    [HttpPost("{id:guid}/status"), ValidateAntiForgeryToken] public async Task<IActionResult> Status(Guid id,ProductionJobStatus status,string? note,CancellationToken ct){try{await service.ChangeStatusAsync(id,status,User.Identity?.Name,note,ct);return RedirectToAction(nameof(Index),new{culture});}catch(InvalidOperationException){return BadRequest("Neplatný přechod stavu výroby.");}}
    private string culture=>RouteData.Values["culture"]?.ToString()??"cs-CZ";
}
