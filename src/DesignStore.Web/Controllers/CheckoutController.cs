using DesignStore.Application.Commerce;
using DesignStore.Domain.Commerce;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DesignStore.Web.Controllers;

[Route("{culture:regex(^cs-CZ|en|de$)}/checkout")]
public sealed class CheckoutController(ICommerceService commerce, IOnlinePaymentService onlinePayments, IWebHostEnvironment environment) : Controller
{
    private const string CookieName = "wildspirit_cart";

    private string Token() => Request.Cookies[CookieName] ?? "";

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("")]
    public async Task<IActionResult> Index(string culture, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(Token())) return RedirectToAction("Index", "Cart", new { culture });
        await PrepareCheckoutView(culture, ct);
        return View(new CheckoutRequest());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string culture, CheckoutRequest request, CancellationToken ct)
    {
        try
        {
            var result = await commerce.CheckoutAsync(Token(), UserId(), request, ct);
            if (request.PaymentMethod == PaymentMethod.Card) return await RedirectToCardPayment(culture, result.OrderId, result.AccessToken, ct);
            return RedirectToAction(nameof(Confirmation), new { culture, id = result.OrderId, token = result.AccessToken });
        }
        catch (ValidationException e)
        {
            AddFriendlyValidationErrors(e);
            await PrepareCheckoutView(culture, ct);
            return View(request);
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Košík je prázdný nebo vypršel.";
            return RedirectToAction("Index", "Cart", new { culture });
        }
    }

    [HttpGet("confirmation/{id:guid}")]
    public async Task<IActionResult> Confirmation(string culture, Guid id, string token, string? payment, CancellationToken ct)
    {
        var order = await commerce.GetCustomerOrderAsync(id, token, ct);
        if (order is null) return NotFound();
        ViewBag.AccessToken = token;
        ViewBag.PaymentReturn = payment;
        ViewBag.AllowCardPayment = onlinePayments.IsEnabled;
        return View(order);
    }

    [HttpPost("pay/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(string culture, Guid id, string token, CancellationToken ct)
    {
        try
        {
            return await RedirectToCardPayment(culture, id, token, ct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<IActionResult> RedirectToCardPayment(string culture, Guid id, string token, CancellationToken ct)
    {
        var successUrl = Url.Action(nameof(Confirmation), "Checkout", new { culture, id, token, payment = "success" }, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Confirmation), "Checkout", new { culture, id, token, payment = "cancelled" }, Request.Scheme)!;
        try
        {
            return Redirect(await onlinePayments.StartAsync(id, token, culture, successUrl, cancelUrl, ct));
        }
        catch (PaymentGatewayException e)
        {
            TempData["Error"] = e.Message;
        }
        catch (ValidationException e)
        {
            TempData["Error"] = e.Message;
        }

        return RedirectToAction(nameof(Confirmation), new { culture, id, token });
    }

    private async Task PrepareCheckoutView(string culture, CancellationToken ct)
    {
        ViewBag.Cart = await commerce.GetCartAsync(Token(), UserId(), culture, ct);
        ViewBag.AllowTestPayment = environment.IsDevelopment();
        ViewBag.AllowCardPayment = onlinePayments.IsEnabled;
    }

    private void AddFriendlyValidationErrors(ValidationException exception)
    {
        // Business errors (e.g. sold out, empty cart) carry only a message and no field errors.
        if (!exception.Errors.Any())
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, FriendlyValidationMessage(error.PropertyName, error.ErrorMessage));
        }
    }

    private static string FriendlyValidationMessage(string propertyName, string fallback)
    {
        var label = propertyName switch
        {
            "Email" => "E-mail",
            "BillingAddress.FullName" => "Jméno",
            "BillingAddress.Street" => "Ulice",
            "BillingAddress.City" => "Město",
            "BillingAddress.PostalCode" => "PSČ",
            "BillingAddress.CountryCode" => "Země",
            "BillingAddress.Phone" => "Telefon",
            "ShippingAddress.FullName" => "Jméno dodací adresy",
            "ShippingAddress.Street" => "Ulice dodací adresy",
            "ShippingAddress.City" => "Město dodací adresy",
            "ShippingAddress.PostalCode" => "PSČ dodací adresy",
            "ShippingAddress.CountryCode" => "Země dodací adresy",
            _ => null
        };

        if (label is null) return fallback;
        if (fallback.Contains("must not be empty", StringComparison.OrdinalIgnoreCase) ||
            fallback.Contains("nesmí být prázdné", StringComparison.OrdinalIgnoreCase))
        {
            return $"{label} je povinné pole.";
        }

        if (fallback.Contains("not in the correct format", StringComparison.OrdinalIgnoreCase) ||
            fallback.Contains("nemá správný formát", StringComparison.OrdinalIgnoreCase) ||
            fallback.Contains("regular expression", StringComparison.OrdinalIgnoreCase))
        {
            return $"{label} nemá správný formát.";
        }

        return fallback;
    }
}
