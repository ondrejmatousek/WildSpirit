using System.Globalization;
using DesignStore.Application;
using DesignStore.Application.Marketing;
using DesignStore.Infrastructure;
using DesignStore.Infrastructure.Persistence;
using DesignStore.Web.Middleware;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews().AddViewLocalization();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddHealthChecks();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminArea", policy => policy.RequireRole("Administrator", "ContentManager", "CatalogEditor", "Warehouse", "Support"))
    .AddPolicy("CatalogManage", policy => policy.RequireRole("Administrator", "ContentManager", "CatalogEditor"));
builder.Services.AddAuthorizationBuilder().AddPolicy("OrderManage", policy => policy.RequireRole("Administrator", "Warehouse", "Support"));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseRouting();
var cultures = new[] { new CultureInfo("cs-CZ"), new CultureInfo("en"), new CultureInfo("de") };
var localization = new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture("cs-CZ"), SupportedCultures = cultures, SupportedUICultures = cultures };
localization.RequestCultureProviders = [new RouteDataRequestCultureProvider { Options = localization }];
app.UseRequestLocalization(localization);
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapControllerRoute("areas", "{culture:regex(^cs-CZ|en|de$)}/{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("content-page", "{culture:regex(^cs-CZ|en|de$)}/Page/{slug}", new { controller = "Content", action = "Page" });
app.MapControllerRoute("gallery", "{culture:regex(^cs-CZ|en|de$)}/Gallery", new { controller = "Content", action = "Gallery" });
app.MapControllerRoute("localized", "{culture:regex(^cs-CZ|en|de$)}/{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nAllow: /\nSitemap: /sitemap.xml\n", "text/plain"));
app.MapGet("/sitemap.xml", async (HttpContext context, IMarketingService marketing, CancellationToken ct) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var urls = await marketing.GetSitemapAsync(baseUrl, ct);
    var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" +
              string.Concat(urls.Select(x => $"<url><loc>{System.Security.SecurityElement.Escape(x.Location)}</loc>{(x.LastModifiedUtc.HasValue ? $"<lastmod>{x.LastModifiedUtc:yyyy-MM-dd}</lastmod>" : "")}</url>")) +
              "</urlset>";
    return Results.Text(xml, "application/xml");
});
app.MapGet("/", () => Results.Redirect("/cs-CZ"));
if (app.Environment.IsDevelopment()) { await using var scope = app.Services.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().RunAsync(); }
app.Run();
public partial class Program;
