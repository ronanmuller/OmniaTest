using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;

namespace Ambev.DeveloperEvaluation.Integration.Products;

public class ProductRepositoryTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DefaultContext(options);
        _repository = new ProductRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync: persists product with rating")]
    public async Task CreateAsync_ValidProduct_PersistsWithRating()
    {
        var product = BuildProduct("Cerveja Brahma", "beverages", 4.50m);

        var created = await _repository.CreateAsync(product);

        var fromDb = await _context.Products.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Title.Should().Be("Cerveja Brahma");
        fromDb.Rating.Should().NotBeNull();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync: returns correct product")]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProduct()
    {
        var product = BuildProduct("Skol Lata", "beverages", 3.80m);
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Skol Lata");
        result.Price.Should().Be(3.80m);
    }

    [Fact(DisplayName = "GetByIdAsync: returns null for unknown id")]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetPagedAsync: filters by category")]
    public async Task GetPagedAsync_WithCategory_ReturnsOnlyMatchingProducts()
    {
        await _context.Products.AddRangeAsync(
            BuildProduct("Cerveja", "beverages", 5m),
            BuildProduct("Refrigerante", "beverages", 4m),
            BuildProduct("Notebook", "electronics", 3000m));
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(1, 10, category: "beverages");

        total.Should().Be(2);
        items.Should().AllSatisfy(p => p.Category.Should().Be("beverages"));
    }

    [Fact(DisplayName = "GetPagedAsync: ordering by price asc works")]
    public async Task GetPagedAsync_OrderByPriceAsc_ReturnsSortedItems()
    {
        await _context.Products.AddRangeAsync(
            BuildProduct("C", "cat", 30m),
            BuildProduct("A", "cat", 10m),
            BuildProduct("B", "cat", 20m));
        await _context.SaveChangesAsync();

        var (items, _) = await _repository.GetPagedAsync(1, 10, "price");

        items[0].Price.Should().Be(10m);
        items[1].Price.Should().Be(20m);
        items[2].Price.Should().Be(30m);
    }

    [Fact(DisplayName = "GetPagedAsync: ordering by price desc works")]
    public async Task GetPagedAsync_OrderByPriceDesc_ReturnsSortedItems()
    {
        await _context.Products.AddRangeAsync(
            BuildProduct("A", "cat", 10m),
            BuildProduct("B", "cat", 20m),
            BuildProduct("C", "cat", 30m));
        await _context.SaveChangesAsync();

        var (items, _) = await _repository.GetPagedAsync(1, 10, "price desc");

        items[0].Price.Should().Be(30m);
        items[2].Price.Should().Be(10m);
    }

    [Fact(DisplayName = "GetPagedAsync: pagination returns correct page")]
    public async Task GetPagedAsync_SecondPage_ReturnsCorrectItems()
    {
        await _context.Products.AddRangeAsync(Enumerable.Range(1, 5)
            .Select(i => BuildProduct($"Produto {i}", "cat", i * 10m)));
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(2, 2);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    // ── GetCategoriesAsync ────────────────────────────────────────────────────

    [Fact(DisplayName = "GetCategoriesAsync: returns distinct sorted categories")]
    public async Task GetCategoriesAsync_MultipleProducts_ReturnsDistinctCategories()
    {
        await _context.Products.AddRangeAsync(
            BuildProduct("A", "beverages", 5m),
            BuildProduct("B", "beverages", 4m),
            BuildProduct("C", "electronics", 100m),
            BuildProduct("D", "food", 10m));
        await _context.SaveChangesAsync();

        var categories = (await _repository.GetCategoriesAsync()).ToList();

        categories.Should().BeEquivalentTo(["beverages", "electronics", "food"]);
        categories.Should().BeInAscendingOrder();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateAsync: persists price change")]
    public async Task UpdateAsync_PriceChange_PersistsCorrectly()
    {
        var product = BuildProduct("Produto", "cat", 10m);
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toUpdate = await _context.Products.FindAsync(product.Id);
        toUpdate!.Price = 15m;
        await _repository.UpdateAsync(toUpdate);

        _context.ChangeTracker.Clear();
        var fromDb = await _context.Products.FindAsync(product.Id);
        fromDb!.Price.Should().Be(15m);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "DeleteAsync: removes product and returns true")]
    public async Task DeleteAsync_ExistingProduct_RemovesAndReturnsTrue()
    {
        var product = BuildProduct("Delete", "cat", 1m);
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(product.Id);

        result.Should().BeTrue();
        (await _context.Products.FindAsync(product.Id)).Should().BeNull();
    }

    [Fact(DisplayName = "DeleteAsync: returns false for unknown id")]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Product BuildProduct(string title, string category, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Category = category,
        Price = price,
        Description = "Test product",
        Image = "http://img.test/img.jpg",
        Rating = new Rating(4.5m, 10),
    };
}
