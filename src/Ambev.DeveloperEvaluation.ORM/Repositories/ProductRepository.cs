using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DefaultContext _context;

    public ProductRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null, string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        query = ApplyOrdering(query, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplyOrdering(IQueryable<Product> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderBy(p => p.Title);

        IOrderedQueryable<Product>? ordered = null;

        foreach (var part in order.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tokens = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = tokens[0].ToLower();
            var desc = tokens.Length > 1 && tokens[1].ToLower() == "desc";

            if (ordered == null)
                ordered = (field, desc) switch
                {
                    ("price", false)    => query.OrderBy(p => p.Price),
                    ("price", true)     => query.OrderByDescending(p => p.Price),
                    ("title", true)     => query.OrderByDescending(p => p.Title),
                    ("category", false) => query.OrderBy(p => p.Category),
                    ("category", true)  => query.OrderByDescending(p => p.Category),
                    _                   => query.OrderBy(p => p.Title)
                };
            else
                ordered = (field, desc) switch
                {
                    ("price", false)    => ordered.ThenBy(p => p.Price),
                    ("price", true)     => ordered.ThenByDescending(p => p.Price),
                    ("title", true)     => ordered.ThenByDescending(p => p.Title),
                    ("category", false) => ordered.ThenBy(p => p.Category),
                    ("category", true)  => ordered.ThenByDescending(p => p.Category),
                    _                   => ordered.ThenBy(p => p.Title)
                };
        }

        return ordered ?? query.OrderBy(p => p.Title);
    }
}
