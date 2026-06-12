using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly CancelSaleItemHandler _handler;

    public CancelSaleItemHandlerTests()
    {
        var correlationId = Substitute.For<ICorrelationIdAccessor>();
        correlationId.CorrelationId.Returns(Guid.NewGuid());
        _handler = new CancelSaleItemHandler(_saleRepository, _eventPublisher, correlationId,
            Substitute.For<ILogger<CancelSaleItemHandler>>());
    }

    [Fact(DisplayName = "Given valid request When cancelling item Then returns success with updated total")]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var targetItem = sale.Items.First();
        sale.CalculateTotals();

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var command = new CancelSaleItemCommand(sale.Id, targetItem.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given valid request When cancelling item Then publishes SaleItemCancelledEvent")]
    public async Task Handle_ValidRequest_PublishesEvent()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var targetItem = sale.Items.First();

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var command = new CancelSaleItemCommand(sale.Id, targetItem.Id);
        await _handler.Handle(command, CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<SaleItemCancelledEvent>(e => e.SaleId == sale.Id && e.ItemId == targetItem.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existent sale When cancelling item Then throws KeyNotFoundException")]
    public async Task Handle_SaleNotFound_ThrowsKeyNotFoundException()
    {
        _saleRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var command = new CancelSaleItemCommand(Guid.NewGuid(), Guid.NewGuid());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Given cancelled sale When cancelling item Then throws DomainException")]
    public async Task Handle_CancelledSale_ThrowsDomainException()
    {
        var sale = SaleTestData.GenerateValidSale();
        sale.Cancel();

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var command = new CancelSaleItemCommand(sale.Id, sale.Items.First().Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*cancelled*");
    }

    [Fact(DisplayName = "Given already cancelled item When cancelling again Then throws DomainException")]
    public async Task Handle_AlreadyCancelledItem_ThrowsDomainException()
    {
        var sale = SaleTestData.GenerateValidSale();
        var targetItem = sale.Items.First();
        targetItem.IsCancelled = true;

        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var command = new CancelSaleItemCommand(sale.Id, targetItem.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<DomainException>()
            .WithMessage("*already cancelled*");
    }
}
