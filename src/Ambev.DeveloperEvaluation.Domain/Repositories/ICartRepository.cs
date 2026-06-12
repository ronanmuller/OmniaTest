using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ICartRepository
{
    Task<Cart> CreateAsync(Cart cart, CancellationToken cancellationToken = default);
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Cart> UpdateAsync(Cart cart, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Cart> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        DateTime? minDate = null, DateTime? maxDate = null,
        CancellationToken cancellationToken = default);
}
