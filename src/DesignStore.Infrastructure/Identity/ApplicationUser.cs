using Microsoft.AspNetCore.Identity;

namespace DesignStore.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string PreferredCulture { get; set; } = "cs-CZ";
    public string PreferredCurrency { get; set; } = "CZK";
}
