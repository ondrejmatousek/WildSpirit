using DesignStore.Application.Commerce;
namespace DesignStore.Tests.Commerce;
public sealed class CommerceValidationTests
{
 [Fact]public async Task Checkout_requires_valid_email_and_address(){var result=await new CheckoutRequestValidator().ValidateAsync(new CheckoutRequest());Assert.False(result.IsValid);Assert.Contains(result.Errors,x=>x.PropertyName=="Email");}
 [Fact]public async Task Cart_quantity_is_bounded(){var result=await new AddToCartRequestValidator().ValidateAsync(new AddToCartRequest(Guid.NewGuid(),null,100,[]));Assert.False(result.IsValid);}
}
