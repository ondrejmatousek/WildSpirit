using DesignStore.Application.Catalog;
using DesignStore.Infrastructure.Catalog;
using DesignStore.Infrastructure.Identity;
using DesignStore.Infrastructure.Persistence;
using DesignStore.Infrastructure.Files;
using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Commerce;
using DesignStore.Application.Marketing;
using DesignStore.Infrastructure.Commerce;
using DesignStore.Infrastructure.Marketing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DesignStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connection));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(o =>
        {
            o.Password.RequiredLength = 12;
            o.Password.RequireNonAlphanumeric = true;
            o.SignIn.RequireConfirmedEmail = true;
            o.Lockout.MaxFailedAccessAttempts = 5;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(o =>
        {
            o.LoginPath = "/cs-CZ/Account/Login";
            o.AccessDeniedPath = "/cs-CZ/Account/AccessDenied";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
        var googleId = configuration["Authentication:Google:ClientId"];
        var googleSecret = configuration["Authentication:Google:ClientSecret"];
        var facebookId = configuration["Authentication:Facebook:AppId"];
        var facebookSecret = configuration["Authentication:Facebook:AppSecret"];
        var authentication = services.AddAuthentication();
        if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret)) authentication.AddGoogle(o => { o.ClientId = googleId; o.ClientSecret = googleSecret; o.SignInScheme = IdentityConstants.ExternalScheme; });
        if (!string.IsNullOrWhiteSpace(facebookId) && !string.IsNullOrWhiteSpace(facebookSecret)) authentication.AddFacebook(o => { o.AppId = facebookId; o.AppSecret = facebookSecret; o.SignInScheme = IdentityConstants.ExternalScheme; });
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        services.AddScoped<DevelopmentSeed>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ICommerceService, CommerceService>();
        services.AddScoped<IInquiryQuoteService, InquiryQuoteService>();
        services.AddScoped<IMarketingService, MarketingService>();
        services.AddScoped<DesignStore.Application.Production.IProductionService, ProductionService>();
        services.AddScoped<IPaymentService, BankTransferPaymentService>();
        services.AddScoped<IPaymentService, TestPaymentService>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
