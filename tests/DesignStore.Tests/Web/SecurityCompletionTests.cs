namespace DesignStore.Tests.Web;

public sealed class SecurityCompletionTests
{
    [Fact]
    public void Program_wires_security_headers_compression_and_health_checks()
    {
        var program = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "Program.cs"));

        Assert.Contains("UseMiddleware<SecurityHeadersMiddleware>", program);
        Assert.Contains("AddResponseCompression", program);
        Assert.Contains("MapHealthChecks(\"/health\")", program);
    }

    [Fact]
    public void Security_headers_middleware_contains_core_hardening_headers()
    {
        var middleware = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "Middleware", "SecurityHeadersMiddleware.cs"));

        Assert.Contains("X-Content-Type-Options", middleware);
        Assert.Contains("X-Frame-Options", middleware);
        Assert.Contains("Content-Security-Policy", middleware);
        Assert.Contains("Permissions-Policy", middleware);
        Assert.Contains("frame-ancestors 'none'", middleware);
    }

    [Fact]
    public void Public_layout_contains_cookie_consent_banner()
    {
        var layout = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("wildspirit_cookie_consent", layout);
        Assert.Contains("asp-action=\"CookieConsent\"", layout);
        Assert.Contains("~/css/privacy.css", layout);
    }

    [Fact]
    public void Docker_compose_requires_external_sql_password_and_exposes_healthchecks()
    {
        var compose = File.ReadAllText(FindRepoFile("compose.yaml"));

        Assert.Contains("WILDSPIRIT_SQL_PASSWORD:?", compose);
        Assert.Contains("/health", compose);
        Assert.Contains("wildspirit-sql", compose);
        Assert.DoesNotContain("WildSpirit.Dev.Sql", compose);
    }

    private static string FindRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Repository file was not found.", Path.Combine(parts));
    }
}
