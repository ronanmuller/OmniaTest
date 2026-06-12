using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        Guid? customerId = null, Guid? branchId = null,
        DateTime? minDate = null, DateTime? maxDate = null, bool? isCancelled = null,
        CancellationToken cancellationToken = default);
}
