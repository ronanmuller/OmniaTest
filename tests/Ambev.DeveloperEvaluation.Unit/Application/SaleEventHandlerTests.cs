using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Application.Common;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class SaleEventHandlerTests
{
    private static readonly Guid CorrelationId = Guid.NewGuid();
    private static readonly Guid CausationId   = CorrelationId;

    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly IIdempotencyGuard _idempotency = Substitute.For<IIdempotencyGuard>();
    private readonly ISaleReadModelRepository _readModel = Substitute.For<ISaleReadModelRepository>();

    [Fact(DisplayName = "SaleCreatedEventHandler handles event without throwing")]
    public async Task SaleCreatedEventHandler_Handle_CompletesSuccessfully()
    {
        var handler = new SaleCreatedEventHandler(
            Substitute.For<ILogger<SaleCreatedEventHandler>>(), _eventStore, _idempotency, _readModel);
        var evt = new SaleCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(), "AB123456",
            Guid.NewGuid(), "John Doe",
            Guid.NewGuid(), "Main Branch",
            500m, DateTime.UtcNow,
            Items: [],
            CorrelationId, CausationId);

        await handler.Handle(evt);

        await _eventStore.Received(1).SaveAsync(
            nameof(SaleCreatedEvent), evt.EventId, evt.SaleId, evt, Arg.Any<CancellationToken>());
        await _readModel.Received(1).UpsertAsync(Arg.Is<SaleReadModel>(m => m.Id == evt.SaleId));
    }

    [Fact(DisplayName = "SaleModifiedEventHandler persists event and updates read model")]
    public async Task SaleModifiedEventHandler_Handle_PersistsEvent()
    {
        var saleId = Guid.NewGuid();
        var existing = new SaleReadModel { Id = saleId, TotalAmount = 500m };
        _readModel.GetByIdAsync(saleId).Returns(existing);

        var handler = new SaleModifiedEventHandler(
            Substitute.For<ILogger<SaleModifiedEventHandler>>(), _eventStore, _idempotency, _readModel);
        var evt = new SaleModifiedEvent(
            Guid.NewGuid(), saleId, "AB123456", 750m, CorrelationId, CausationId);

        await handler.Handle(evt);

        await _eventStore.Received(1).SaveAsync(
            nameof(SaleModifiedEvent), evt.EventId, evt.SaleId, evt, Arg.Any<CancellationToken>());
        await _readModel.Received(1).UpsertAsync(Arg.Is<SaleReadModel>(m => m.TotalAmount == 750m));
    }

    [Fact(DisplayName = "SaleCancelledEventHandler persists event and marks read model cancelled")]
    public async Task SaleCancelledEventHandler_Handle_PersistsEvent()
    {
        var handler = new SaleCancelledEventHandler(
            Substitute.For<ILogger<SaleCancelledEventHandler>>(), _eventStore, _idempotency, _readModel);
        var evt = new SaleCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), "AB123456", CorrelationId, CausationId);

        await handler.Handle(evt);

        await _eventStore.Received(1).SaveAsync(
            nameof(SaleCancelledEvent), evt.EventId, evt.SaleId, evt, Arg.Any<CancellationToken>());
        await _readModel.Received(1).SetCancelledAsync(evt.SaleId);
    }

    [Fact(DisplayName = "SaleItemCancelledEventHandler persists event and marks item cancelled in read model")]
    public async Task SaleItemCancelledEventHandler_Handle_PersistsEvent()
    {
        var handler = new SaleItemCancelledEventHandler(
            Substitute.For<ILogger<SaleItemCancelledEventHandler>>(), _eventStore, _idempotency, _readModel);
        var evt = new SaleItemCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), "AB123456", Guid.NewGuid(), "Widget Pro",
            CorrelationId, CausationId);

        await handler.Handle(evt);

        await _eventStore.Received(1).SaveAsync(
            nameof(SaleItemCancelledEvent), evt.EventId, evt.SaleId, evt, Arg.Any<CancellationToken>());
        await _readModel.Received(1).SetItemCancelledAsync(evt.SaleId, evt.ItemId);
    }
}
