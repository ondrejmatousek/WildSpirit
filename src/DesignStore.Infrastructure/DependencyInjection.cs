using DesignStore.Application.Catalog;
using DesignStore.Infrastructure.Catalog;
using DesignStore.Infrastructure.Identity;
using DesignStore.Infrastructure.Persistence;
using DesignStore.Infrastructure.Files;
using DesignStore.Application.Abstractions.Files;
using DesignStore.Application.Commerce;
using DesignStore.Infrastructure.Commerce;
using Microsoft.AspNetCore.Identity;
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
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        services.AddScoped<DevelopmentSeed>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ICommerceService, CommerceService>();
        services.AddScoped<IPaymentService, BankTransferPaymentService>();
        services.AddScoped<IPaymentService, TestPaymentService>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
