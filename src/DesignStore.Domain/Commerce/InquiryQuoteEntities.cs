using DesignStore.Domain.Catalog;
using DesignStore.Domain.Common;

namespace DesignStore.Domain.Commerce;

public enum InquiryStatus { New, InReview, QuoteSent, Accepted, Rejected, ConvertedToOrder, Closed }
public enum QuoteStatus { Draft, Sent, Accepted, Rejected, ChangeRequested, Expired, ConvertedToOrder, Cancelled }
public enum InquiryNoteVisibility { Internal, Customer }

public sealed class ProductInquiry : AuditableEntity
{
    public string Number { get; set; } = "";
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public InquiryStatus Status { get; set; } = InquiryStatus.New;
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string CountryCode { get; set; } = "CZ";
    public int Quantity { get; set; } = 1;
    public string? RequestedDeadline { get; set; }
    public string? Note { get; set; }
    public bool TermsAccepted { get; set; }
    public string ProductName { get; set; } = "";
    public string VariantName { get; set; } = "";
    public string Sku { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal? CatalogPrice { get; set; }
    public decimal VatRate { get; set; }
    public string Currency { get; set; } = "CZK";
    public decimal ExchangeRate { get; set; } = 1;
    public List<ProductInquiryConfigurationItem> Configuration { get; set; } = [];
    public List<ProductInquiryAttachment> Attachments { get; set; } = [];
    public List<ProductInquiryStatusHistory> StatusHistory { get; set; } = [];
    public List<ProductInquiryNote> Notes { get; set; } = [];
    public List<Quote> Quotes { get; set; } = [];
}
public sealed class ProductInquiryConfigurationItem : AuditableEntity
{ public Guid ProductInquiryId { get; set; } public ProductInquiry ProductInquiry { get; set; } = null!; public string OptionName { get; set; } = ""; public string Value { get; set; } = ""; public decimal PriceAdjustment { get; set; } }
public sealed class ProductInquiryAttachment : AuditableEntity
{ public Guid ProductInquiryId { get; set; } public ProductInquiry ProductInquiry { get; set; } = null!; public string StorageKey { get; set; } = ""; public string OriginalName { get; set; } = ""; public string ContentType { get; set; } = ""; public long Length { get; set; } public string Sha256 { get; set; } = ""; public bool IsSafe { get; set; } }
public sealed class ProductInquiryStatusHistory : AuditableEntity
{ public Guid ProductInquiryId { get; set; } public ProductInquiry ProductInquiry { get; set; } = null!; public InquiryStatus? PreviousStatus { get; set; } public InquiryStatus NewStatus { get; set; } public string Reason { get; set; } = ""; public string? CreatedByName { get; set; } }
public sealed class ProductInquiryNote : AuditableEntity
{ public Guid ProductInquiryId { get; set; } public ProductInquiry ProductInquiry { get; set; } = null!; public InquiryNoteVisibility Visibility { get; set; } public string Body { get; set; } = ""; public string? Author { get; set; } }
public sealed class Quote : AuditableEntity
{
    public string Number { get; set; } = ""; public Guid ProductInquiryId { get; set; } public ProductInquiry ProductInquiry { get; set; } = null!;
    public int Version { get; set; } public QuoteStatus Status { get; set; } = QuoteStatus.Draft; public string Currency { get; set; } = "CZK"; public decimal ExchangeRate { get; set; } = 1;
    public decimal Subtotal { get; set; } public decimal TaxTotal { get; set; } public decimal DiscountTotal { get; set; } public decimal DepositAmount { get; set; } public decimal Total { get; set; }
    public string? ProductionTime { get; set; } public DateTime? ValidUntilUtc { get; set; } public string Terms { get; set; } = ""; public string? Note { get; set; } public Guid? ConvertedOrderId { get; set; }
    public List<QuoteItem> Items { get; set; } = []; public List<QuoteStatusHistory> StatusHistory { get; set; } = []; public List<QuoteAccessToken> AccessTokens { get; set; } = [];
}
public sealed class QuoteItem : AuditableEntity
{ public Guid QuoteId { get; set; } public Quote Quote { get; set; } = null!; public string Description { get; set; } = ""; public int Quantity { get; set; } = 1; public decimal UnitPrice { get; set; } public decimal VatRate { get; set; } public decimal LineTotal { get; set; } }
public sealed class QuoteStatusHistory : AuditableEntity
{ public Guid QuoteId { get; set; } public Quote Quote { get; set; } = null!; public QuoteStatus? PreviousStatus { get; set; } public QuoteStatus NewStatus { get; set; } public string Reason { get; set; } = ""; public string? CreatedByName { get; set; } }
public sealed class QuoteAccessToken : AuditableEntity
{ public Guid QuoteId { get; set; } public Quote Quote { get; set; } = null!; public string TokenHash { get; set; } = ""; public string Purpose { get; set; } = "Customer"; public DateTime ExpiresAtUtc { get; set; } public DateTime? UsedAtUtc { get; set; } public DateTime? RevokedAtUtc { get; set; } }
