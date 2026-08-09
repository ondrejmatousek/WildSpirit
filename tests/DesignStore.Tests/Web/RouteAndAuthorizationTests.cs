using DesignStore.Web.Areas.Admin.Controllers;
using DesignStore.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Tests.Web;

public sealed class RouteAndAuthorizationTests
{
    [Fact]
    public void Product_detail_has_localized_public_route()
    {
        var method = typeof(CatalogController).GetMethod(nameof(CatalogController.Detail))!;
        var route = Assert.Single(method.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>());
        Assert.Contains("{culture", route.Template);
        Assert.Contains("{slug}", route.Template);
    }

    [Fact]
    public void Admin_dashboard_requires_admin_policy()
    {
        var authorize = Assert.Single(typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("AdminArea", authorize.Policy);
        Assert.NotNull(typeof(DashboardController).GetCustomAttributes(typeof(AreaAttribute), true).SingleOrDefault());
    }

    [Fact] public void Product_administration_requires_catalog_policy() { var authorize = Assert.Single(typeof(ProductsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()); Assert.Equal("CatalogManage", authorize.Policy); }

    [Fact]
    public void Media_upload_accepts_multiple_files_and_requires_catalog_policy()
    {
        var authorize = Assert.Single(typeof(DesignStore.Web.Areas.Admin.Controllers.MediaController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("CatalogManage", authorize.Policy);

        var upload = typeof(DesignStore.Web.Areas.Admin.Controllers.MediaController).GetMethod(nameof(DesignStore.Web.Areas.Admin.Controllers.MediaController.Upload))!;
        Assert.Single(upload.GetCustomAttributes(typeof(HttpPostAttribute), true));
        Assert.Contains(upload.GetParameters(), p => p.Name == "files" && p.ParameterType == typeof(List<IFormFile>));
    }

    [Fact]
    public void Account_page_and_logout_require_authenticated_user()
    {
        var account = typeof(AccountController).GetMethod(nameof(AccountController.Index))!;
        var logout = typeof(AccountController).GetMethod(nameof(AccountController.Logout))!;

        Assert.Single(account.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Single(logout.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(typeof(AccountController).GetMethod(nameof(AccountController.Login), [typeof(string)])!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    [Fact]
    public void Privacy_export_and_deletion_require_authenticated_user()
    {
        var export = typeof(PrivacyController).GetMethod(nameof(PrivacyController.Export))!;
        var deletion = typeof(PrivacyController).GetMethod(nameof(PrivacyController.RequestDeletion))!;
        var consent = typeof(PrivacyController).GetMethod(nameof(PrivacyController.CookieConsent))!;

        Assert.Single(export.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Single(deletion.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Single(consent.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        Assert.Single(deletion.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
    }
}
