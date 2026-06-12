using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface IProductRepository
{
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null, string? category = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
