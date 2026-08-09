namespace DesignStore.Domain.Commerce;

public static class CommerceRules
{
    public static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal Discount(DiscountType type, decimal value, decimal subtotal) => RoundMoney(type == DiscountType.Percentage ? subtotal * Math.Clamp(value, 0, 100) / 100m : Math.Min(value, subtotal));
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch { (OrderStatus.PendingPayment, OrderStatus.Paid or OrderStatus.Cancelled) => true, (OrderStatus.Paid, OrderStatus.Processing or OrderStatus.Cancelled) => true, (OrderStatus.Processing, OrderStatus.Shipped or OrderStatus.Cancelled) => true, (OrderStatus.Shipped, OrderStatus.Completed) => true, _ => false };
}
