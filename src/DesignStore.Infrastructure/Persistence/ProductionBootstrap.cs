using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DesignStore.Infrastructure.Persistence;

/// <summary>
/// Production start-up: applies pending migrations, ensures the roles exist and, when Bootstrap:AdminEmail and
/// Bootstrap:AdminPassword are supplied, creates the first administrator. It never creates demo data and never
/// changes an existing account's password.
/// </summary>
public sealed class ProductionBootstrap(ApplicationDbContext db, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles, IConfiguration configuration, ILogger<ProductionBootstrap> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        foreach (var role in DevelopmentSeed.Roles)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));

        var email = configuration["Bootstrap:AdminEmail"]?.Trim();
        if (string.IsNullOrEmpty(email)) return;
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            var password = configuration["Bootstrap:AdminPassword"];
            if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Bootstrap:AdminPassword is required to create the first administrator.");
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded) throw new InvalidOperationException("The first administrator could not be created: " + string.Join("; ", created.Errors.Select(x => x.Description)));
            logger.LogInformation("Created the first administrator account.");
        }
        if (!await users.IsInRoleAsync(user, "Administrator")) await users.AddToRoleAsync(user, "Administrator");
    }
}
