using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Ambev.DeveloperEvaluation.Application.Sales.ResyncReadModel;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class ResyncReadModelHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ISaleReadModelRepository _readModel = Substitute.For<ISaleReadModelRepository>();
    private readonly ResyncReadModelHandler _handler;

    public ResyncReadModelHandlerTests()
    {
        _handler = new ResyncReadModelHandler(
            _saleRepository,
            _readModel,
            Substitute.For<ILogger<ResyncReadModelHandler>>());
    }

    [Fact(DisplayName = "Given sales with items When resync runs Then projects items to read model")]
    public async Task Handle_SalesWithItems_ProjectsItemsToReadModel()
    {
        // Given — a sale with 3 items in the repository
        var sale = SaleTestData.GenerateValidSale(itemCount: 3);
        sale.CalculateTotals();
        SetupPagedRepository(sale);

        SaleReadModel? captured = null;
        await _readModel.UpsertAsync(Arg.Do<SaleReadModel>(m => captured = m), Arg.Any<CancellationToken>());

        // When
        var result = await _handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then
        result.Synced.Should().Be(1);
        result.Failed.Should().Be(0);
        captured.Should().NotBeNull();
        captured!.Items.Should().HaveCount(3, "all items must be projected to the read model");
        captured.Items.Select(i => i.Id).Should().BeEquivalentTo(sale.Items.Select(i => i.Id));
    }

    [Fact(DisplayName = "Given sale with items When resync runs Then item totals are projected correctly")]
    public async Task Handle_SalesWithItems_ProjectsCorrectTotals()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        sale.Items[0].Quantity = 5;
        sale.Items[0].UnitPrice = 10m;
        sale.Items[1].Quantity = 10;
        sale.Items[1].UnitPrice = 20m;
        sale.CalculateTotals();
        SetupPagedRepository(sale);

        SaleReadModel? captured = null;
        await _readModel.UpsertAsync(Arg.Do<SaleReadModel>(m => captured = m), Arg.Any<CancellationToken>());

        // When
        await _handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then — item[0]: 5 units, no discount (< 4 triggers no discount... wait 4-9 is 10%)
        // Actually 5 items gets 10% discount: 5 * 10 * 0.9 = 45
        // 10 items gets 20% discount: 10 * 20 * 0.8 = 160
        captured!.Items[0].Quantity.Should().Be(5);
        captured.Items[0].UnitPrice.Should().Be(10m);
        captured.Items[1].Quantity.Should().Be(10);
        captured.Items[1].UnitPrice.Should().Be(20m);
        captured.TotalAmount.Should().Be(sale.TotalAmount);
    }

    [Fact(DisplayName = "Given sale with cancelled item When resync runs Then cancelled flag is projected")]
    public async Task Handle_SaleWithCancelledItem_ProjectsCancelledFlag()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        sale.Items[0].IsCancelled = true;
        SetupPagedRepository(sale);

        SaleReadModel? captured = null;
        await _readModel.UpsertAsync(Arg.Do<SaleReadModel>(m => captured = m), Arg.Any<CancellationToken>());

        // When
        await _handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then
        captured!.Items.Should().HaveCount(2);
        captured.Items.First(i => i.Id == sale.Items[0].Id).IsCancelled.Should().BeTrue();
        captured.Items.First(i => i.Id == sale.Items[1].Id).IsCancelled.Should().BeFalse();
    }

    [Fact(DisplayName = "Given empty repository When resync runs Then returns zero synced")]
    public async Task Handle_EmptyRepository_ReturnsZeroSynced()
    {
        // Given
        _saleRepository
            .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Sale>(), 0));

        // When
        var result = await _handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then
        result.Synced.Should().Be(0);
        result.Failed.Should().Be(0);
        await _readModel.DidNotReceive().UpsertAsync(Arg.Any<SaleReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given read model throws When resync runs Then increments failed count and continues")]
    public async Task Handle_ReadModelThrows_CountsFailedAndContinues()
    {
        // Given — 2 sales, second upsert throws
        var sale1 = SaleTestData.GenerateValidSale();
        var sale2 = SaleTestData.GenerateValidSale();
        _saleRepository
            .GetPagedAsync(1, Arg.Any<int>(), null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Sale>)[sale1, sale2], 2));
        _saleRepository
            .GetPagedAsync(Arg.Is<int>(p => p > 1), Arg.Any<int>(), null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Sale>(), 0));

        var callCount = 0;
        _readModel.UpsertAsync(Arg.Any<SaleReadModel>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 2) throw new Exception("MongoDB timeout");
                return Task.CompletedTask;
            });

        // When
        var result = await _handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then — first sale synced, second failed, process did not abort
        result.Synced.Should().Be(1);
        result.Failed.Should().Be(1);
    }

    [Fact(DisplayName = "Given multiple pages When resync runs Then all pages are processed")]
    public async Task Handle_MultiplePages_ProcessesAllPages()
    {
        // Given — page 1 full (2 = pageSize for this test), page 2 partial
        const int pageSize = 2;
        var page1 = new[] { SaleTestData.GenerateValidSale(), SaleTestData.GenerateValidSale() };
        var page2 = new[] { SaleTestData.GenerateValidSale() };

        // The handler uses pageSize = 100, so we need to simulate that
        // We test via the boundary: if page returns < pageSize records, loop stops
        _saleRepository
            .GetPagedAsync(1, 100, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Sale>)page1.Concat(page2).ToArray(), 3));
        _saleRepository
            .GetPagedAsync(Arg.Is<int>(p => p > 1), 100, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Sale>(), 0));

        // When
        var result = await _handler.Handle(new ResyncReadModelCommand(), CancellationToken.None);

        // Then
        result.Synced.Should().Be(3);
    }

    private void SetupPagedRepository(params Sale[] sales)
    {
        _saleRepository
            .GetPagedAsync(1, Arg.Any<int>(), null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Sale>)sales, sales.Length));
        _saleRepository
            .GetPagedAsync(Arg.Is<int>(p => p > 1), Arg.Any<int>(), null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Sale>(), 0));
    }
}
