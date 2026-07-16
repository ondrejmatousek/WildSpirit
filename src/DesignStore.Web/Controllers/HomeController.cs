using System.Diagnostics;
using DesignStore.Application.Catalog;
using DesignStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Controllers;
public sealed class HomeController(ICatalogService catalog) : Controller
{
    public async Task<IActionResult> Index(string culture = "cs-CZ", CancellationToken ct = default) => View(await catalog.GetHomePageAsync(culture, ct));
    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
