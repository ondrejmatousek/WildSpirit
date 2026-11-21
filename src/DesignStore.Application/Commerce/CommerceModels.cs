using DesignStore.Domain.Commerce;
using FluentValidation;
namespace DesignStore.Application.Commerce;

public sealed record CartConfigurationInput(Guid ProductOptionId, Guid? ProductOptionValueId, string? CustomText);
public sealed record AddToCartRequest(Guid ProductId, Guid? ProductVariantId, int Quantity, List<CartConfigurationInput> Configuration);
public sealed record CartLine(Guid Id, string Name, string Variant, string Sku, string ImageUrl, int Quantity, decimal UnitPrice, decimal Total, IReadOnlyList<string> Configuration);
public sealed record CartView(Guid Id, IReadOnlyList<CartLine> Items, decimal Subtotal, decimal Discount, decimal Total, string Currency, string? DiscountCode);
public sealed class AddressInput { public string FullName { get; set; } = ""; public string? Company { get; set; } = ""; public string Street { get; set; } = ""; public string City { get; set; } = ""; public string PostalCode { get; set; } = ""; public string CountryCode { get; set; } = "CZ"; public string? Phone { get; set; } = ""; }
public sealed class CheckoutRequest { public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N"); public string Email { get; set; } = ""; public AddressInput BillingAddress { get; set; } = new(); public AddressInput ShippingAddress { get; set; } = new(); public bool UseBillingAsShipping { get; set; } = true; public string ShippingMethod { get; set; } = "standard"; public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer; }
public sealed record CheckoutResult(Guid OrderId, string OrderNumber, decimal Total, string Currency, PaymentStatus PaymentStatus, string AccessToken);
public sealed record OrderListItem(Guid Id, string Number, string Email, DateTime CreatedAtUtc, OrderStatus Status, PaymentStatus PaymentStatus, decimal Total, string Currency);
public sealed class OrderContactUpdateRequest { public string Email { get; set; } = ""; public AddressInput BillingAddress { get; set; } = new(); public AddressInput ShippingAddress { get; set; } = new(); }
public sealed record OrderAddressView(AddressType Type, string FullName, string Company, string Street, string City, string PostalCode, string CountryCode, string Phone);
public sealed record OrderInventoryMovementView(string Type, string Sku, string ItemName, int QuantityChange, int PreviousQuantity, int NewQuantity, string Reason, DateTime CreatedAtUtc);
public sealed record GiftVoucherCodeView(string Code, decimal Value, bool IsActive, int UsedCount, int? MaximumUses);
public sealed record OrderDetail(Guid Id, string Number, string Email, DateTime CreatedAtUtc, OrderStatus Status, PaymentStatus PaymentStatus, decimal Subtotal, decimal Discount, decimal Shipping, decimal Tax, decimal Total, string Currency, string ShippingMethod, IReadOnlyList<CartLine> Items, IReadOnlyList<OrderAddressView> Addresses, IReadOnlyList<OrderInventoryMovementView> InventoryMovements, IReadOnlyList<string> History, IReadOnlyList<GiftVoucherCodeView> GiftVoucherCodes, PaymentMethod? PaymentMethod = null);
public interface ICommerceService
{
    Task<CartView> GetCartAsync(string token, Guid? userId, string culture, CancellationToken ct);
    Task AddAsync(string token, Guid? userId, string culture, AddToCartRequest request, CancellationToken ct);
    Task UpdateQuantityAsync(string token, Guid? userId, Guid itemId, int quantity, CancellationToken ct);
    Task RemoveAsync(string token, Guid? userId, Guid itemId, CancellationToken ct);
    Task ApplyDiscountAsync(string token, Guid? userId, string code, CancellationToken ct);
    Task<CheckoutResult> CheckoutAsync(string token, Guid? userId, CheckoutRequest request, CancellationToken ct);
    Task<IReadOnlyList<OrderListItem>> GetOrdersAsync(CancellationToken ct);
    Task<OrderDetail?> GetOrderAsync(Guid id, CancellationToken ct); Task<OrderDetail?> GetCustomerOrderAsync(Guid id, string accessToken, CancellationToken ct);
    Task UpdateOrderContactAsync(Guid id, OrderContactUpdateRequest request, string? actor, CancellationToken ct);
    Task CancelAsync(Guid id, string? actor, CancellationToken ct);
    Task ChangeStatusAsync(Guid id, OrderStatus status, string? actor, CancellationToken ct);
    /// <summary>Records a verified online payment. Idempotent per event; returns false when the event does not match the order (unknown order, amount or currency).</summary>
    Task<bool> ConfirmOnlinePaymentAsync(OnlinePaymentEvent paymentEvent, CancellationToken ct);
    /// <summary>Cancels a still-unpaid card order (returning stock) when its latest payment page expired.</summary>
    Task<bool> ExpireOnlinePaymentAsync(OnlinePaymentEvent paymentEvent, CancellationToken ct);
}
public interface IPaymentService { PaymentMethod Method { get; } Task<PaymentResult> CreateAsync(string orderNumber, decimal total, string currency, string idempotencyKey, CancellationToken ct); }
public sealed record PaymentResult(PaymentStatus Status, string Reference, string ExternalEventId);
public sealed class AddToCartRequestValidator : AbstractValidator<AddToCartRequest> { public AddToCartRequestValidator() { RuleFor(x => x.ProductId).NotEmpty(); RuleFor(x => x.Quantity).InclusiveBetween(1, 99); } }
public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest> { public CheckoutRequestValidator() { RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100); RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320); RuleFor(x => x.ShippingMethod).Must(x => x is "standard" or "pickup"); RuleFor(x => x.PaymentMethod).IsInEnum();RuleFor(x => x.BillingAddress).SetValidator(new AddressInputValidator()); When(x => !x.UseBillingAsShipping, () => RuleFor(x => x.ShippingAddress).SetValidator(new AddressInputValidator())); } }
public sealed class AddressInputValidator : AbstractValidator<AddressInput> { public AddressInputValidator() { RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.Street).NotEmpty().MaximumLength(250); RuleFor(x => x.City).NotEmpty().MaximumLength(120); RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20); RuleFor(x => x.CountryCode).Matches("^[A-Z]{2}$"); RuleFor(x => x.Phone).MaximumLength(40).Matches(@"^(\+420\s?)?[1-9][0-9]{2}\s?[0-9]{3}\s?[0-9]{3}$").When(x => !string.IsNullOrWhiteSpace(x.Phone)); } }
