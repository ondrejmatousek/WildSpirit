using DesignStore.Domain.Commerce;
namespace DesignStore.Tests.Commerce;

public sealed class CommerceRulesTests
{
    [Fact] public void Percentage_discount_is_rounded_away_from_zero() => Assert.Equal(33.34m, CommerceRules.Discount(DiscountType.Percentage, 10, 333.35m));
    [Fact] public void Fixed_discount_never_exceeds_subtotal() => Assert.Equal(100m, CommerceRules.Discount(DiscountType.FixedAmount, 500, 100));
    [Fact] public void Completed_order_cannot_be_cancelled() => Assert.False(CommerceRules.CanTransition(OrderStatus.Completed, OrderStatus.Cancelled));
    [Fact] public void Sent_quote_can_be_accepted_but_converted_quote_cannot_be_reopened() { Assert.True(QuoteRules.CanTransition(QuoteStatus.Sent, QuoteStatus.Accepted)); Assert.False(QuoteRules.CanTransition(QuoteStatus.ConvertedToOrder, QuoteStatus.Sent)); }
    [Fact] public void Expired_quote_is_not_usable() => Assert.False(QuoteRules.IsUsable(DateTime.UtcNow.AddMinutes(-1), QuoteStatus.Sent, DateTime.UtcNow));
}
