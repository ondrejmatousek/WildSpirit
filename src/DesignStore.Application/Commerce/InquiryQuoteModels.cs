using DesignStore.Domain.Commerce;

namespace DesignStore.Application.Commerce;

public sealed record InquiryConfigurationInput(string OptionName, string Value, decimal PriceAdjustment = 0);
public sealed record CreateInquiryRequest(Guid ProductId, Guid? ProductVariantId, string Email, string FullName, string CountryCode, int Quantity, string? RequestedDeadline, string? Note, bool TermsAccepted, IReadOnlyList<InquiryConfigurationInput> Configuration);
public sealed record InquirySummary(Guid Id, string Number, InquiryStatus Status, string ProductName, string Email, DateTime CreatedAtUtc);
public sealed record QuoteSummary(Guid Id, string Number, int Version, QuoteStatus Status, decimal Total, string Currency, DateTime? ValidUntilUtc);
public sealed record QuoteView(Guid Id, string Number, int Version, QuoteStatus Status, decimal Subtotal, decimal TaxTotal, decimal DepositAmount, decimal Total, string Currency, DateTime? ValidUntilUtc, IReadOnlyList<string> Items, bool CanRespond);
public interface IInquiryQuoteService
{
    Task<ProductInquiry> CreateInquiryAsync(CreateInquiryRequest request, CancellationToken ct);
    Task AddAttachmentAsync(Guid inquiryId, DesignStore.Application.Abstractions.Files.FileUpload upload, CancellationToken ct);
    Task<IReadOnlyList<InquirySummary>> GetInquiriesAsync(CancellationToken ct);
    Task<Quote> CreateQuoteAsync(Guid inquiryId, IReadOnlyList<(string Description, int Quantity, decimal UnitPrice, decimal VatRate)> items, decimal deposit, DateTime validUntilUtc, string terms, CancellationToken ct);
    Task<string> SendQuoteAsync(Guid quoteId, CancellationToken ct);
    Task<QuoteView?> GetQuoteAsync(string token, CancellationToken ct);
    Task RespondToQuoteAsync(string token, QuoteStatus response, string? comment, CancellationToken ct);
    Task<Quote> ConvertQuoteToOrderAsync(string token, string idempotencyKey, CancellationToken ct);
}
