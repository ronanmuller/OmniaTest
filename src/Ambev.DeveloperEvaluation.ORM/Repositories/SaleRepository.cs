using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale == null) return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        Guid? customerId = null, Guid? branchId = null,
        DateTime? minDate = null, DateTime? maxDate = null, bool? isCancelled = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sales.Include(s => s.Items).AsNoTracking().AsQueryable();

        query = ApplyFilters(query, customerId, branchId, minDate, maxDate, isCancelled);
        query = ApplyOrdering(query, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private static IQueryable<Sale> ApplyFilters(
        IQueryable<Sale> query, Guid? customerId, Guid? branchId,
        DateTime? minDate, DateTime? maxDate, bool? isCancelled)
    {
        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        if (minDate.HasValue)
            query = query.Where(s => s.Date >= minDate.Value);

        if (maxDate.HasValue)
            query = query.Where(s => s.Date <= maxDate.Value);

        if (isCancelled.HasValue)
            query = query.Where(s => s.IsCancelled == isCancelled.Value);

        return query;
    }

    private static IQueryable<Sale> ApplyOrdering(IQueryable<Sale> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderByDescending(s => s.Date);

        IOrderedQueryable<Sale>? ordered = null;

        foreach (var part in order.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tokens = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = tokens[0].ToLower();
            var desc = tokens.Length > 1 && tokens[1].ToLower() == "desc";

            if (ordered == null)
                ordered = (field, desc) switch
                {
                    ("date", false)         => query.OrderBy(s => s.Date),
                    ("date", true)          => query.OrderByDescending(s => s.Date),
                    ("totalamount", false)  => query.OrderBy(s => s.TotalAmount),
                    ("totalamount", true)   => query.OrderByDescending(s => s.TotalAmount),
                    ("customername", false) => query.OrderBy(s => s.CustomerName),
                    ("customername", true)  => query.OrderByDescending(s => s.CustomerName),
                    ("salenumber", false)   => query.OrderBy(s => s.SaleNumber),
                    ("salenumber", true)    => query.OrderByDescending(s => s.SaleNumber),
                    _                       => query.OrderByDescending(s => s.Date)
                };
            else
                ordered = (field, desc) switch
                {
                    ("date", false)         => ordered.ThenBy(s => s.Date),
                    ("date", true)          => ordered.ThenByDescending(s => s.Date),
                    ("totalamount", false)  => ordered.ThenBy(s => s.TotalAmount),
                    ("totalamount", true)   => ordered.ThenByDescending(s => s.TotalAmount),
                    ("customername", false) => ordered.ThenBy(s => s.CustomerName),
                    ("customername", true)  => ordered.ThenByDescending(s => s.CustomerName),
                    ("salenumber", false)   => ordered.ThenBy(s => s.SaleNumber),
                    ("salenumber", true)    => ordered.ThenByDescending(s => s.SaleNumber),
                    _                       => ordered.ThenByDescending(s => s.Date)
                };
        }

        return ordered ?? query.OrderByDescending(s => s.Date);
    }
}
