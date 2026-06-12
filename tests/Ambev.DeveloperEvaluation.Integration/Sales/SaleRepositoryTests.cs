using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Application.Sales.ResyncReadModel;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

/// <summary>
/// Integration tests for SaleRepository using EF Core InMemory.
/// These tests exercise the actual EF queries — including .Include() calls —
/// which unit tests with mocked repositories cannot catch.
/// </summary>
public class SaleRepositoryTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly SaleRepository _repository;

    public SaleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultContext(options);
        _repository = new SaleRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync: returns sale with items populated")]
    public async Task GetByIdAsync_ExistingSaleWithItems_ReturnsItemsPopulated()
    {
        // Given — a sale with 3 items saved to the DB
        var sale = BuildSale(itemCount: 3);
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        // When
        var result = await _repository.GetByIdAsync(sale.Id);

        // Then
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3, "GetByIdAsync must use .Include(s => s.Items)");
        result.Items.Select(i => i.Id).Should().BeEquivalentTo(sale.Items.Select(i => i.Id));
    }

    [Fact(DisplayName = "GetByIdAsync: returns null for unknown id")]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetByIdAsync: returns sale with cancelled items")]
    public async Task GetByIdAsync_SaleWithCancelledItem_ReturnsCancelledFlag()
    {
        // Given
        var sale = BuildSale(itemCount: 2);
        sale.Items[0].IsCancelled = true;
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        // When
        var result = await _repository.GetByIdAsync(sale.Id);

        // Then
        result!.Items.First(i => i.Id == sale.Items[0].Id).IsCancelled.Should().BeTrue();
        result.Items.First(i => i.Id == sale.Items[1].Id).IsCancelled.Should().BeFalse();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetPagedAsync: returns sales with items populated")]
    public async Task GetPagedAsync_ExistingSalesWithItems_ReturnsItemsPopulated()
    {
        // Given — THIS IS THE BUG TEST:
        // Before the fix, GetPagedAsync lacked .Include(s => s.Items) so Items was always empty.
        var sale = BuildSale(itemCount: 2);
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        // When
        var (items, total) = await _repository.GetPagedAsync(1, 10);

        // Then
        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Items.Should().HaveCount(2,
            "GetPagedAsync must use .Include(s => s.Items) — this test would have caught the missing Include bug");
    }

    [Fact(DisplayName = "GetPagedAsync: pagination returns correct page")]
    public async Task GetPagedAsync_MultiplePages_ReturnsCorrectPage()
    {
        // Given — 5 sales
        var sales = Enumerable.Range(1, 5).Select(i => BuildSale(itemCount: 1)).ToList();
        await _context.Sales.AddRangeAsync(sales);
        await _context.SaveChangesAsync();

        // When — page 2 with size 2
        var (items, total) = await _repository.GetPagedAsync(2, 2);

        // Then
        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetPagedAsync: last page returns remaining items")]
    public async Task GetPagedAsync_LastPage_ReturnsRemainingItems()
    {
        // Given — 3 sales, page size 2 → last page has 1
        var sales = Enumerable.Range(1, 3).Select(_ => BuildSale(itemCount: 1)).ToList();
        await _context.Sales.AddRangeAsync(sales);
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(2, 2);

        total.Should().Be(3);
        items.Should().HaveCount(1);
    }

    [Fact(DisplayName = "GetPagedAsync: each sale's items are fully loaded")]
    public async Task GetPagedAsync_MultipleSalesWithItems_AllItemsLoaded()
    {
        // Given — 2 sales, one with 1 item and one with 3 items
        var sale1 = BuildSale(itemCount: 1);
        var sale2 = BuildSale(itemCount: 3);
        await _context.Sales.AddRangeAsync(sale1, sale2);
        await _context.SaveChangesAsync();

        // When
        var (items, _) = await _repository.GetPagedAsync(1, 10);

        // Then
        items.Should().HaveCount(2);
        var loaded1 = items.Single(s => s.Id == sale1.Id);
        var loaded2 = items.Single(s => s.Id == sale2.Id);
        loaded1.Items.Should().HaveCount(1);
        loaded2.Items.Should().HaveCount(3);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync: persists sale with all items")]
    public async Task CreateAsync_SaleWithItems_PersistsAllItems()
    {
        // Given
        var sale = BuildSale(itemCount: 3);

        // When
        var created = await _repository.CreateAsync(sale);

        // Then — items must be in the DB, not just in memory
        var fromDb = await _context.Sales.Include(s => s.Items).FirstAsync(s => s.Id == created.Id);
        fromDb.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "CreateAsync: item discounts are persisted")]
    public async Task CreateAsync_SaleWithDiscountItems_PersistsDiscounts()
    {
        // Given — 5 items triggers 10% discount
        var sale = BuildSale(itemCount: 0);
        sale.Items.Add(new SaleItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductTitle = "Prod", UnitPrice = 100m, Quantity = 5 });
        sale.CalculateTotals();

        // When
        var created = await _repository.CreateAsync(sale);

        // Then
        var fromDb = await _context.Sales.Include(s => s.Items).FirstAsync(s => s.Id == created.Id);
        fromDb.Items[0].Discount.Should().Be(0.10m);
        fromDb.Items[0].TotalAmount.Should().Be(450m); // 5 * 100 * 0.9
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateAsync: persists item cancellation")]
    public async Task UpdateAsync_CancelItem_PersistsCancelledFlag()
    {
        // Given
        var sale = BuildSale(itemCount: 2);
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // When — cancel the first item
        var loaded = await _context.Sales.Include(s => s.Items).FirstAsync(s => s.Id == sale.Id);
        loaded.CancelItem(loaded.Items[0].Id);
        await _repository.UpdateAsync(loaded);

        // Then
        _context.ChangeTracker.Clear();
        var result = await _context.Sales.Include(s => s.Items).FirstAsync(s => s.Id == sale.Id);
        result.Items.First(i => i.Id == sale.Items[0].Id).IsCancelled.Should().BeTrue();
        result.Items.First(i => i.Id == sale.Items[1].Id).IsCancelled.Should().BeFalse();
    }

    // ── ResyncReadModelHandler integration ────────────────────────────────────

    [Fact(DisplayName = "ResyncReadModel: projects items from repository to read model")]
    public async Task ResyncReadModelHandler_SaleWithItems_ProjectsItemsToReadModel()
    {
        // Given — this test combines real EF (no mock) + mocked MongoDB
        var sale = BuildSale(itemCount: 3);
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        var readModel = Substitute.For<ISaleReadModelRepository>();
        SaleReadModel? captured = null;
        await readModel.UpsertAsync(Arg.Do<SaleReadModel>(m => captured = m), Arg.Any<CancellationToken>());

        var handler = new ResyncReadModelHandler(
            _repository,
            readModel,
            Substitute.For<ILogger<ResyncReadModelHandler>>());

        // When
        var result = await handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then
        result.Synced.Should().Be(1);
        result.Failed.Should().Be(0);
        captured.Should().NotBeNull("read model must be projected");
        captured!.Items.Should().HaveCount(3,
            "ResyncReadModelHandler must receive sales with items from GetPagedAsync — " +
            "this test would have caught the missing .Include() in GetPagedAsync");
    }

    [Fact(DisplayName = "ResyncReadModel: projects correct item data including discounts")]
    public async Task ResyncReadModelHandler_ItemsWithDiscount_ProjectsDiscountedTotals()
    {
        // Given — 10 units triggers 20% discount
        var sale = BuildSale(itemCount: 0);
        sale.Items.Add(new SaleItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductTitle = "Prod", UnitPrice = 50m, Quantity = 10 });
        sale.CalculateTotals();
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        var readModel = Substitute.For<ISaleReadModelRepository>();
        SaleReadModel? captured = null;
        await readModel.UpsertAsync(Arg.Do<SaleReadModel>(m => captured = m), Arg.Any<CancellationToken>());

        var handler = new ResyncReadModelHandler(_repository, readModel, Substitute.For<ILogger<ResyncReadModelHandler>>());

        // When
        await handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then — 10 * 50 * 0.8 = 400
        captured!.Items[0].Discount.Should().Be(0.20m);
        captured.Items[0].TotalAmount.Should().Be(400m);
        captured.TotalAmount.Should().Be(400m);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Sale BuildSale(int itemCount)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = $"VND-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            Date = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            BranchId = Guid.NewGuid(),
            BranchName = "Test Branch",
        };

        for (var i = 0; i < itemCount; i++)
        {
            sale.Items.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductTitle = $"Product {i + 1}",
                UnitPrice = 10m * (i + 1),
                Quantity = 2,
            });
        }

        sale.CalculateTotals();
        return sale;
    }
}
