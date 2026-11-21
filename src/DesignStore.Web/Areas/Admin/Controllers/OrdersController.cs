using DesignStore.Application.Commerce;
using DesignStore.Domain.Commerce;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesignStore.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "OrderManage")]
public sealed class OrdersController(ICommerceService commerce) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await commerce.GetOrdersAsync(ct));

    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var order = await commerce.GetOrderAsync(id, ct);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(Guid id, OrderContactUpdateRequest request, CancellationToken ct)
    {
        try
        {
            await commerce.UpdateOrderContactAsync(id, request, User.Identity?.Name, ct);

            if (IsAjax())
            {
                return Json(new
                {
                    ok = true,
                    email = request.Email,
                    billing = request.BillingAddress,
                    shipping = request.ShippingAddress
                });
            }

            TempData["Success"] = "Kontakt a adresy objednávky byly uloženy.";
        }
        catch (ValidationException e)
        {
            var errors = FriendlyValidationErrors(e);

            if (IsAjax())
            {
                return BadRequest(new
                {
                    ok = false,
                    error = "Zkontrolujte zvýrazněná pole.",
                    errors
                });
            }

            TempData["Error"] = string.Join(" ", errors.Select(x => x.Message));
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(Guid id, OrderStatus status, CancellationToken ct)
    {
        try
        {
            await commerce.ChangeStatusAsync(id, status, User.Identity?.Name, ct);
            TempData["Success"] = "Stav objednávky byl změněn.";
        }
        catch (ValidationException e)
        {
            TempData["Error"] = e.Message;
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            await commerce.CancelAsync(id, User.Identity?.Name, ct);
            TempData["Success"] = "Objednávka byla zrušena a sklad vrácen.";
        }
        catch (ValidationException e)
        {
            TempData["Error"] = e.Message;
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    bool IsAjax() => Request.Headers.XRequestedWith == "XMLHttpRequest" ||
                     Request.Headers.Accept.Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

    static IReadOnlyList<FieldError> FriendlyValidationErrors(ValidationException exception)
    {
        var errors = exception.Errors?.Select(error =>
            new FieldError(error.PropertyName, FriendlyValidationMessage(error.PropertyName, error.ErrorMessage))).ToList();

        return errors is { Count: > 0 }
            ? errors
            : [new FieldError("", exception.Message)];
    }

    static string FriendlyValidationMessage(string field, string fallback)
    {
        var label = field switch
        {
            "Email" => "E-mail",
            "BillingAddress.FullName" => "Jméno ve fakturační adrese",
            "BillingAddress.Street" => "Ulice ve fakturační adrese",
            "BillingAddress.City" => "Město ve fakturační adrese",
            "BillingAddress.PostalCode" => "PSČ ve fakturační adrese",
            "BillingAddress.CountryCode" => "Země ve fakturační adrese",
            "BillingAddress.Phone" => "Telefon ve fakturační adrese",
            "ShippingAddress.FullName" => "Jméno v dodací adrese",
            "ShippingAddress.Street" => "Ulice v dodací adrese",
            "ShippingAddress.City" => "Město v dodací adrese",
            "ShippingAddress.PostalCode" => "PSČ v dodací adrese",
            "ShippingAddress.CountryCode" => "Země v dodací adrese",
            "ShippingAddress.Phone" => "Telefon v dodací adrese",
            _ => string.IsNullOrWhiteSpace(field) ? "Pole" : field
        };

        if (fallback.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            fallback.Contains("prázd", StringComparison.OrdinalIgnoreCase))
        {
            return $"{label} je povinné pole.";
        }

        if (fallback.Contains("format", StringComparison.OrdinalIgnoreCase) ||
            fallback.Contains("formát", StringComparison.OrdinalIgnoreCase))
        {
            return $"{label} nemá správný formát.";
        }

        return fallback.Replace(field, label, StringComparison.OrdinalIgnoreCase);
    }

    sealed record FieldError(string Field, string Message);
}
