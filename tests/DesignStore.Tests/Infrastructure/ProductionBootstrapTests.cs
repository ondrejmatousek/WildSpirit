using DesignStore.Infrastructure.Identity;
using DesignStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DesignStore.Tests.Infrastructure;

[Collection("PostgreSQL database")]
public sealed class ProductionBootstrapTests(PostgresDatabaseFixture database)
{
    [Fact]
    public async Task Bootstrap_creates_roles_and_first_admin_once_without_demo_data()
    {
        var email = $"owner-{Guid.NewGuid():N}@example.test";
        await RunBootstrapAsync(new() { ["Bootstrap:AdminEmail"] = email, ["Bootstrap:AdminPassword"] = "Production.Owner.2026!" });
        // Second start: password no longer supplied, existing account stays untouched.
        await RunBootstrapAsync(new() { ["Bootstrap:AdminEmail"] = email });

        await using var services = Services([]);
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        Assert.True(await users.IsInRoleAsync(user, "Administrator"));
        Assert.True(await users.CheckPasswordAsync(user, "Production.Owner.2026!"));
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in DevelopmentSeed.Roles) Assert.True(await roles.RoleExistsAsync(role));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().DiscountCodes.Where(x => x.Code == "WELCOME10").ToListAsync());
    }

    [Fact]
    public async Task Bootstrap_without_password_for_new_admin_fails_clearly()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => RunBootstrapAsync(new() { ["Bootstrap:AdminEmail"] = $"missing-{Guid.NewGuid():N}@example.test" }));
        Assert.Contains("Bootstrap:AdminPassword", error.Message);
    }

    async Task RunBootstrapAsync(Dictionary<string, string?> settings)
    {
        await using var services = Services(settings);
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ProductionBootstrap>().RunAsync();
    }

    ServiceProvider Services(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(database.ConnectionString));
        services.AddIdentityCore<ApplicationUser>(o => { o.Password.RequiredLength = 12; o.Password.RequireNonAlphanumeric = true; })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<ProductionBootstrap>();
        return services.BuildServiceProvider();
    }
}
