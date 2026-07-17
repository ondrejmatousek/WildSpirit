using System.ComponentModel.DataAnnotations;
using DesignStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Controllers;

public sealed class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginModel { ReturnUrl = returnUrl });

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
