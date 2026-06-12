using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class CartRepository : ICartRepository
{
    private readonly DefaultContext _context;

    public CartRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Cart> CreateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cart> UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Carts
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == cart.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cart {cart.Id} not found");

        existing.UserId = cart.UserId;
        existing.Date = cart.Date;

        var oldItems = await _context.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync(cancellationToken);
        _context.CartItems.RemoveRange(oldItems);

        var newItems = cart.Products.Select(p => new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = p.ProductId,
            Quantity = p.Quantity
        }).ToList();
        await _context.CartItems.AddRangeAsync(newItems, cancellationToken);
        existing.Products = newItems;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cart == null) return false;

        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Cart> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        DateTime? minDate = null, DateTime? maxDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Carts.Include(c => c.Products).AsNoTracking().AsQueryable();

        if (minDate.HasValue)
            query = query.Where(c => c.Date >= minDate.Value);

        if (maxDate.HasValue)
            query = query.Where(c => c.Date <= maxDate.Value);

        query = ApplyOrdering(query, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private static IQueryable<Cart> ApplyOrdering(IQueryable<Cart> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderByDescending(c => c.Date);

        IOrderedQueryable<Cart>? ordered = null;

        foreach (var part in order.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tokens = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = tokens[0].ToLower();
            var desc = tokens.Length > 1 && tokens[1].ToLower() == "desc";

            if (ordered == null)
                ordered = (field, desc) switch
                {
                    ("date", false)   => query.OrderBy(c => c.Date),
                    ("date", true)    => query.OrderByDescending(c => c.Date),
                    ("userid", false) => query.OrderBy(c => c.UserId),
                    ("userid", true)  => query.OrderByDescending(c => c.UserId),
                    _                 => query.OrderByDescending(c => c.Date)
                };
            else
                ordered = (field, desc) switch
                {
                    ("date", false)   => ordered.ThenBy(c => c.Date),
                    ("date", true)    => ordered.ThenByDescending(c => c.Date),
                    ("userid", false) => ordered.ThenBy(c => c.UserId),
                    ("userid", true)  => ordered.ThenByDescending(c => c.UserId),
                    _                 => ordered.ThenByDescending(c => c.Date)
                };
        }

        return ordered ?? query.OrderByDescending(c => c.Date);
    }
}
