using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DefaultContext _context;

    public UserRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        query = ApplyOrdering(query, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private static IQueryable<User> ApplyOrdering(IQueryable<User> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderBy(u => u.Username);

        IOrderedQueryable<User>? ordered = null;

        foreach (var part in order.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tokens = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = tokens[0].ToLower();
            var desc = tokens.Length > 1 && tokens[1].ToLower() == "desc";

            if (ordered == null)
                ordered = (field, desc) switch
                {
                    ("username", true)   => query.OrderByDescending(u => u.Username),
                    ("email", false)     => query.OrderBy(u => u.Email),
                    ("email", true)      => query.OrderByDescending(u => u.Email),
                    ("firstname", false) => query.OrderBy(u => u.Firstname),
                    ("firstname", true)  => query.OrderByDescending(u => u.Firstname),
                    _                    => query.OrderBy(u => u.Username)
                };
            else
                ordered = (field, desc) switch
                {
                    ("username", true)   => ordered.ThenByDescending(u => u.Username),
                    ("email", false)     => ordered.ThenBy(u => u.Email),
                    ("email", true)      => ordered.ThenByDescending(u => u.Email),
                    ("firstname", false) => ordered.ThenBy(u => u.Firstname),
                    ("firstname", true)  => ordered.ThenByDescending(u => u.Firstname),
                    _                    => ordered.ThenBy(u => u.Username)
                };
        }

        return ordered ?? query.OrderBy(u => u.Username);
    }
}
