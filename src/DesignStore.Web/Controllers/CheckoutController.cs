using System.Security.Claims;
using DesignStore.Application.Commerce;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Controllers;
[Route("{culture:regex(^cs-CZ|en|de$)}/checkout")]
public sealed class CheckoutController(ICommerceService commerce,IWebHostEnvironment environment):Controller
{
 const string CookieName="wildspirit_cart";string Token()=>Request.Cookies[CookieName]??"";Guid? UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
 [HttpGet("")]public async Task<IActionResult> Index(string culture,CancellationToken ct){if(string.IsNullOrEmpty(Token()))return RedirectToAction("Index","Cart",new{culture});ViewBag.Cart=await commerce.GetCartAsync(Token(),UserId(),culture,ct);ViewBag.AllowTestPayment=environment.IsDevelopment();return View(new CheckoutRequest());}
 [HttpPost(""),ValidateAntiForgeryToken]public async Task<IActionResult> Index(string culture,CheckoutRequest request,CancellationToken ct){try{var result=await commerce.CheckoutAsync(Token(),UserId(),request,ct);return RedirectToAction(nameof(Confirmation),new{culture,id=result.OrderId,token=result.AccessToken});}catch(ValidationException e){ModelState.AddModelError("",e.Message);ViewBag.Cart=await commerce.GetCartAsync(Token(),UserId(),culture,ct);ViewBag.AllowTestPayment=environment.IsDevelopment();return View(request);}}
 [HttpGet("confirmation/{id:guid}")]public async Task<IActionResult> Confirmation(string culture,Guid id,string token,CancellationToken ct){var order=await commerce.GetCustomerOrderAsync(id,token,ct);return order is null?NotFound():View(order);}
}
