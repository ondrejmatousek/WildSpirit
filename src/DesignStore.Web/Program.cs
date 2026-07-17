using System.Globalization;
using DesignStore.Application;
using DesignStore.Infrastructure;
using DesignStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews().AddViewLocalization();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminArea", policy => policy.RequireRole("Administrator", "ContentManager", "CatalogEditor", "Warehouse", "Support"))
    .AddPolicy("CatalogManage", policy => policy.RequireRole("Administrator", "ContentManager", "CatalogEditor"));
builder.Services.AddAuthorizationBuilder().AddPolicy("OrderManage", policy => policy.RequireRole("Administrator", "Warehouse", "Support"));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseRouting();
var cultures = new[] { new CultureInfo("cs-CZ"), new CultureInfo("en"), new CultureInfo("de") };
var localization = new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture("cs-CZ"), SupportedCultures = cultures, SupportedUICultures = cultures };
localization.RequestCultureProviders = [new RouteDataRequestCultureProvider { Options = localization }];
app.UseRequestLocalization(localization);
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute("areas", "{culture:regex(^cs-CZ|en|de$)}/{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("localized", "{culture:regex(^cs-CZ|en|de$)}/{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapGet("/", () => Results.Redirect("/cs-CZ"));
if (app.Environment.IsDevelopment()) { await using var scope = app.Services.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().RunAsync(); }
app.Run();
public partial class Program;
