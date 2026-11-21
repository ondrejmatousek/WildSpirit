using DesignStore.Domain.Common;

namespace DesignStore.Domain.Commerce;

public enum ProductionJobStatus { Planned, InProgress, QualityCheck, Ready, Shipped, Completed, Cancelled }
public enum ProductionAttachmentVisibility { Internal, Customer }

public sealed class ProductionJob : AuditableEntity
{
    public string Number { get; set; } = "";
    public Guid OrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public Guid? UserId { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string SkuSnapshot { get; set; } = "";
    public int Quantity { get; set; }
    public ProductionJobStatus Status { get; set; } = ProductionJobStatus.Planned;
    public DateTime? PlannedStartUtc { get; set; }
    public DateTime? PlannedCompletionUtc { get; set; }
    public DateTime? ActualCompletionUtc { get; set; }
    public string? ResponsibleUserId { get; set; }
    public List<ProductionJobStatusHistory> StatusHistory { get; set; } = [];
    public List<ProductionJobAttachment> Attachments { get; set; } = [];
    public List<ProductionJobNote> Notes { get; set; } = [];
}
public sealed class ProductionJobStatusHistory : AuditableEntity
{
    public Guid ProductionJobId { get; set; }
    public ProductionJob ProductionJob { get; set; } = null!;
    public ProductionJobStatus FromStatus { get; set; }
    public ProductionJobStatus ToStatus { get; set; }
    public string? Actor { get; set; }
    public string? Note { get; set; }
}
public sealed class ProductionJobAttachment : AuditableEntity
{
    public Guid ProductionJobId { get; set; }
    public ProductionJob ProductionJob { get; set; } = null!;
    public string StorageKey { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public string Sha256 { get; set; } = "";
    public ProductionAttachmentVisibility Visibility { get; set; }
}
public sealed class ProductionJobNote : AuditableEntity
{
    public Guid ProductionJobId { get; set; }
    public ProductionJob ProductionJob { get; set; } = null!;
    public string Body { get; set; } = "";
    public bool CustomerVisible { get; set; }
}
public sealed class CustomerAddress : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Label { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Company { get; set; } = "";
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string CountryCode { get; set; } = "CZ";
    public string Phone { get; set; } = "";
    public bool IsDefaultBilling { get; set; }
    public bool IsDefaultShipping { get; set; }
}
public sealed class FavoriteProduct : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}

public static class ProductionJobRules
{
    public static bool CanTransition(ProductionJobStatus from, ProductionJobStatus to) => from switch
    {
        ProductionJobStatus.Planned => to is ProductionJobStatus.InProgress or ProductionJobStatus.Cancelled,
        ProductionJobStatus.InProgress => to is ProductionJobStatus.QualityCheck or ProductionJobStatus.Cancelled,
        ProductionJobStatus.QualityCheck => to is ProductionJobStatus.Ready or ProductionJobStatus.InProgress,
        ProductionJobStatus.Ready => to is ProductionJobStatus.Shipped or ProductionJobStatus.Completed,
        ProductionJobStatus.Shipped => to is ProductionJobStatus.Completed,
        _ => false
    };
}
