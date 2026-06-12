using Ambev.DeveloperEvaluation.Domain.ReadModels;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Read-side repository for Sale.
/// Reads from MongoDB — may reflect a state a few seconds behind PostgreSQL.
/// Falls back to null when the projection hasn't arrived yet (eventual consistency window).
/// </summary>
public interface ISaleReadModelRepository
{
    Task<SaleReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SaleReadModel> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        Guid? customerId = null, Guid? branchId = null,
        DateTime? minDate = null, DateTime? maxDate = null, bool? isCancelled = null,
        CancellationToken cancellationToken = default);
    Task UpsertAsync(SaleReadModel model, CancellationToken cancellationToken = default);
    Task SetCancelledAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task SetItemCancelledAsync(Guid saleId, Guid itemId, CancellationToken cancellationToken = default);
}
