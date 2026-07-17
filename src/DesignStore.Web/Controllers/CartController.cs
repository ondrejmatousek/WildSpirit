using System.Security.Claims;
using DesignStore.Application.Commerce;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
namespace DesignStore.Web.Controllers;
[Route("{culture:regex(^cs-CZ|en|de$)}/cart")]
public sealed class CartController(ICommerceService commerce):Controller
{
 const string CookieName="wildspirit_cart";
 string Token(){if(Request.Cookies.TryGetValue(CookieName,out var value)&&value.Length>=32)return value;value=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));Response.Cookies.Append(CookieName,value,new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Lax,Secure=Request.IsHttps,MaxAge=TimeSpan.FromDays(30),IsEssential=true});return value;}
 Guid? UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
 [HttpGet("")]public async Task<IActionResult> Index(string culture,CancellationToken ct)=>View(await commerce.GetCartAsync(Token(),UserId(),culture,ct));
 [HttpPost("add"),ValidateAntiForgeryToken]public async Task<IActionResult> Add(string culture,AddToCartRequest request,CancellationToken ct){try{await commerce.AddAsync(Token(),UserId(),culture,request,ct);TempData["Success"]="Produkt byl přidán do košíku.";return RedirectToAction(nameof(Index),new{culture});}catch(ValidationException e){TempData["Error"]=e.Message;return Redirect(Request.Headers.Referer.ToString());}}
 [HttpPost("quantity"),ValidateAntiForgeryToken]public async Task<IActionResult> Quantity(string culture,Guid itemId,int quantity,CancellationToken ct){await commerce.UpdateQuantityAsync(Token(),UserId(),itemId,quantity,ct);return RedirectToAction(nameof(Index),new{culture});}
 [HttpPost("remove"),ValidateAntiForgeryToken]public async Task<IActionResult> Remove(string culture,Guid itemId,CancellationToken ct){await commerce.RemoveAsync(Token(),UserId(),itemId,ct);return RedirectToAction(nameof(Index),new{culture});}
 [HttpPost("discount"),ValidateAntiForgeryToken]public async Task<IActionResult> Discount(string culture,string code,CancellationToken ct){try{await commerce.ApplyDiscountAsync(Token(),UserId(),code,ct);TempData["Success"]="Slevový kód byl použit.";}catch(ValidationException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index),new{culture});}
}
