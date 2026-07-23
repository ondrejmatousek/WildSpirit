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
    public void Public_layout_logout_is_post_form_and_mobile_css_keeps_it_clickable()
    {
        var loginPartial = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "Views", "Shared", "_LoginPartial.cshtml"));
        var accountPage = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "Views", "Account", "Index.cshtml"));
        var vintageCss = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "wwwroot", "css", "vintage.css"));

        Assert.Contains("asp-action=\"Logout\"", loginPartial);
        Assert.Contains("method=\"post\"", loginPartial);
        Assert.Contains("<button type=\"submit\"", loginPartial);
        Assert.Contains("asp-action=\"Logout\"", accountPage);
        Assert.Contains("Odhlásit se", accountPage);
        Assert.Contains("cursor: pointer", vintageCss);
        Assert.Contains(".account-nav button:hover", vintageCss);
        Assert.Contains(".account-nav a:not(:first-child) { display: none; }", vintageCss);
        Assert.DoesNotContain(".account-nav a:not(:first-child), .account-nav form { display: none; }", vintageCss);
    }

    [Fact]
    public void Admin_layout_logout_posts_to_account_logout_and_looks_clickable()
    {
        var adminLayout = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "Areas", "Admin", "Views", "Shared", "_AdminLayout.cshtml"));
        var adminCss = File.ReadAllText(FindRepoFile("src", "DesignStore.Web", "wwwroot", "css", "admin.css"));

        Assert.Contains("Url.Action(\"Logout\", \"Account\"", adminLayout);
        Assert.Contains("method=\"post\"", adminLayout);
        Assert.Contains("@Html.AntiForgeryToken()", adminLayout);
        Assert.Contains("admin-logout-button", adminLayout);
        Assert.Contains("cursor:pointer", adminCss);
        Assert.DoesNotContain("<form method=\"post\" action=\"\">", adminLayout);
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
