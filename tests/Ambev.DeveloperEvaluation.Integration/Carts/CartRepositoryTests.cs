using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;

namespace Ambev.DeveloperEvaluation.Integration.Carts;

public class CartRepositoryTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly CartRepository _repository;

    public CartRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DefaultContext(options);
        _repository = new CartRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync: persists cart with products")]
    public async Task CreateAsync_CartWithProducts_PersistsProducts()
    {
        var cart = BuildCart(productCount: 3);

        var created = await _repository.CreateAsync(cart);

        var fromDb = await _context.Carts.Include(c => c.Products).FirstAsync(c => c.Id == created.Id);
        fromDb.Products.Should().HaveCount(3);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync: returns cart with products populated")]
    public async Task GetByIdAsync_CartWithProducts_ReturnsProductsPopulated()
    {
        var cart = BuildCart(productCount: 2);
        await _context.Carts.AddAsync(cart);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(cart.Id);

        result.Should().NotBeNull();
        result!.Products.Should().HaveCount(2, "GetByIdAsync must use .Include(c => c.Products)");
    }

    [Fact(DisplayName = "GetByIdAsync: returns null for unknown id")]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetPagedAsync: returns carts with products populated")]
    public async Task GetPagedAsync_CartsWithProducts_ReturnsProductsPopulated()
    {
        var cart = BuildCart(productCount: 3);
        await _context.Carts.AddAsync(cart);
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(1, 10);

        total.Should().Be(1);
        items[0].Products.Should().HaveCount(3);
    }

    [Fact(DisplayName = "GetPagedAsync: filters by minDate")]
    public async Task GetPagedAsync_WithMinDate_ReturnsOnlyCartsAfterDate()
    {
        var cutoff = DateTime.UtcNow;
        var old = BuildCart(productCount: 1);
        old.Date = cutoff.AddDays(-2);
        var recent = BuildCart(productCount: 1);
        recent.Date = cutoff.AddDays(1);

        await _context.Carts.AddRangeAsync(old, recent);
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(1, 10, minDate: cutoff);

        total.Should().Be(1);
        items[0].Id.Should().Be(recent.Id);
    }

    [Fact(DisplayName = "GetPagedAsync: filters by maxDate")]
    public async Task GetPagedAsync_WithMaxDate_ReturnsOnlyCartsBeforeDate()
    {
        var cutoff = DateTime.UtcNow;
        var old = BuildCart(productCount: 1);
        old.Date = cutoff.AddDays(-1);
        var future = BuildCart(productCount: 1);
        future.Date = cutoff.AddDays(2);

        await _context.Carts.AddRangeAsync(old, future);
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(1, 10, maxDate: cutoff);

        total.Should().Be(1);
        items[0].Id.Should().Be(old.Id);
    }

    [Fact(DisplayName = "GetPagedAsync: pagination works correctly")]
    public async Task GetPagedAsync_SecondPage_ReturnsCorrectCount()
    {
        await _context.Carts.AddRangeAsync(Enumerable.Range(1, 5).Select(_ => BuildCart(1)));
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(2, 2);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateAsync: replaces products list")]
    public async Task UpdateAsync_NewProductsList_ReplacesOldProducts()
    {
        var cart = BuildCart(productCount: 2);
        await _context.Carts.AddAsync(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Replace with a single new product
        var newProductId = Guid.NewGuid();
        var updatedCart = new Cart
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Date = cart.Date,
            Products = [new CartItem { ProductId = newProductId, Quantity = 5 }]
        };
        await _repository.UpdateAsync(updatedCart);

        _context.ChangeTracker.Clear();
        var fromDb = await _context.Carts.Include(c => c.Products).FirstAsync(c => c.Id == cart.Id);
        fromDb.Products.Should().HaveCount(1, "update must replace the products list, not append");
        fromDb.Products[0].ProductId.Should().Be(newProductId);
    }

    [Fact(DisplayName = "UpdateAsync: throws KeyNotFoundException for unknown cart")]
    public async Task UpdateAsync_UnknownCart_ThrowsKeyNotFoundException()
    {
        var cart = new Cart { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Date = DateTime.UtcNow };

        await _repository.Invoking(r => r.UpdateAsync(cart))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "DeleteAsync: removes cart and products")]
    public async Task DeleteAsync_CartWithProducts_RemovesCartAndProducts()
    {
        var cart = BuildCart(productCount: 2);
        await _context.Carts.AddAsync(cart);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(cart.Id);

        result.Should().BeTrue();
        (await _context.Carts.FindAsync(cart.Id)).Should().BeNull();
        _context.CartItems.Where(ci => ci.CartId == cart.Id).Should().BeEmpty();
    }

    [Fact(DisplayName = "DeleteAsync: returns false for unknown id")]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Cart BuildCart(int productCount)
    {
        var cartId = Guid.NewGuid();
        return new Cart
        {
            Id = cartId,
            UserId = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Products = Enumerable.Range(1, productCount).Select(i => new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                ProductId = Guid.NewGuid(),
                Quantity = i,
            }).ToList()
        };
    }
}
