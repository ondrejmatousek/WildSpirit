using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Policy = "AdminArea")]
public sealed class DashboardController : Controller { public IActionResult Index() => View(); }
