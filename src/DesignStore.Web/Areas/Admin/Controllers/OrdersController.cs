using DesignStore.Application.Commerce;
using DesignStore.Domain.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Policy="OrderManage")]
public sealed class OrdersController(ICommerceService commerce):Controller
{
 public async Task<IActionResult> Index(CancellationToken ct)=>View(await commerce.GetOrdersAsync(ct));
 public async Task<IActionResult> Detail(Guid id,CancellationToken ct){var o=await commerce.GetOrderAsync(id,ct);return o is null?NotFound():View(o);}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Status(Guid id,OrderStatus status,CancellationToken ct){await commerce.ChangeStatusAsync(id,status,User.Identity?.Name,ct);TempData["Success"]="Stav objednávky byl změněn.";return RedirectToAction(nameof(Detail),new{id});}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Cancel(Guid id,CancellationToken ct){await commerce.CancelAsync(id,User.Identity?.Name,ct);TempData["Success"]="Objednávka byla zrušena a sklad vrácen.";return RedirectToAction(nameof(Detail),new{id});}
}
