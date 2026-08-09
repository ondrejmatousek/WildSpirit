namespace DesignStore.Web.Models;

public sealed record GiftVoucherPageModel(
    Guid ProductId,
    string Name,
    string Description,
    string ImageUrl,
    IReadOnlyList<GiftVoucherAmountOption> Amounts);

public sealed record GiftVoucherAmountOption(Guid VariantId, string Name, decimal Price);