using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DesignStore.Web.Controllers;

public sealed class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null) { ViewBag.ExternalLogins = await signInManager.GetExternalAuthenticationSchemesAsync(); return View(new LoginModel { ReturnUrl = returnUrl }); }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null) { var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { culture = CurrentCulture(), returnUrl }); var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl!); return Challenge(properties, provider); }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null) { if (remoteError is not null) { TempData["Error"] = "Externí přihlášení se nezdařilo."; return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl }); } var info = await signInManager.GetExternalLoginInfoAsync(); if (info is null) { TempData["Error"] = "Externí přihlášení neposkytlo údaje."; return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl }); } var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true); if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : $"/{CurrentCulture()}"); var email = info.Principal.FindFirstValue(ClaimTypes.Email); if (string.IsNullOrWhiteSpace(email)) { TempData["Error"] = "Poskytovatel nevrátil e-mail."; return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl }); } var user = await userManager.FindByEmailAsync(email); if (user is null) { user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = string.Equals(info.Principal.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase) }; var created = await userManager.CreateAsync(user); if (!created.Succeeded) { TempData["Error"] = string.Join(" ", created.Errors.Select(x => x.Description)); return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl }); } } var linked = await userManager.AddLoginAsync(user, info); if (!linked.Succeeded && !linked.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.LoginAlreadyAssociated))) { TempData["Error"] = "Externí účet nelze připojit."; return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl }); } await signInManager.SignInAsync(user, false, info.LoginProvider); logger.LogInformation("External login for {Email} via {Provider}", email, info.LoginProvider); return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : $"/{CurrentCulture()}"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl!);
        if (result.Succeeded) return RedirectToAction(nameof(Index), new { culture = CurrentCulture() });
        ModelState.AddModelError(string.Empty, "Přihlášení se nezdařilo.");
        return View(model);
    }

    [Authorize, HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { culture = CurrentCulture() });
    }

    private string CurrentCulture() => RouteData.Values["culture"]?.ToString() ?? "cs-CZ";
}

public sealed class LoginModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
