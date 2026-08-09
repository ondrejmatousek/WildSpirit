using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DesignStore.Web.Controllers;

public sealed class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        await LoadExternalLoginsAsync();
        return View(new LoginModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { culture = CurrentCulture(), returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl!);
        return Challenge(properties, provider);
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            TempData["Error"] = "Externí přihlášení se nezdařilo.";
            return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl });
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["Error"] = "Externí přihlášení neposkytlo údaje.";
            return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl });
        }

        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
        if (result.Succeeded) return LocalRedirect(SafeReturnUrl(returnUrl));

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Poskytovatel nevrátil e-mail.";
            return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl });
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = string.Equals(info.Principal.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase)
            };

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                TempData["Error"] = string.Join(" ", created.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl });
            }
        }

        var linked = await userManager.AddLoginAsync(user, info);
        if (!linked.Succeeded && !linked.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.LoginAlreadyAssociated)))
        {
            TempData["Error"] = "Externí účet nelze připojit.";
            return RedirectToAction(nameof(Login), new { culture = CurrentCulture(), returnUrl });
        }

        await signInManager.SignInAsync(user, false, info.LoginProvider);
        logger.LogInformation("External login for {Email} via {Provider}", email, info.LoginProvider);
        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadExternalLoginsAsync();
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(SafeReturnUrl(model.ReturnUrl));

        await LoadExternalLoginsAsync();
        ModelState.AddModelError(string.Empty, "Přihlášení se nezdařilo.");
        return View(model);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register(string? returnUrl = null) => View(new RegisterModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(RegisterModel.Email), "Účet s tímto e-mailem už existuje. Přihlaste se nebo použijte obnovu hesla.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var created = await userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await signInManager.SignInAsync(user, model.RememberMe);
        logger.LogInformation("Local registration completed for {Email}", email);
        return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
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

    private async Task LoadExternalLoginsAsync() => ViewBag.ExternalLogins = await signInManager.GetExternalAuthenticationSchemesAsync();

    private string SafeReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl! : $"/{CurrentCulture()}";

    private string CurrentCulture() => RouteData.Values["culture"]?.ToString() ?? "cs-CZ";
}

public sealed class LoginModel
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Zapamatovat přihlášení")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterModel
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(12, ErrorMessage = "Heslo musí mít alespoň 12 znaků.")]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Hesla se neshodují.")]
    [Display(Name = "Potvrzení hesla")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Zapamatovat přihlášení")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}