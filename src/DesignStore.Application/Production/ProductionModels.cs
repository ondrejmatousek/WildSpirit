using DesignStore.Domain.Commerce;
namespace DesignStore.Application.Production;

public sealed record ProductionSummary(Guid Id, string Number, string ProductName, ProductionJobStatus Status, DateTime? PlannedCompletionUtc);
public interface IProductionService { Task<IReadOnlyList<ProductionSummary>> GetForCustomerAsync(Guid userId, CancellationToken ct); Task<ProductionJob?> GetCustomerJobAsync(Guid userId, Guid id, CancellationToken ct); Task<ProductionJob> ChangeStatusAsync(Guid id, ProductionJobStatus status, string? actor, string? note, CancellationToken ct); }
