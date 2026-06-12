using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

public class SaleItemCancelledEventHandler : DomainEventHandlerBase<SaleItemCancelledEvent>
{
    private readonly ISaleReadModelRepository _readModel;

    public SaleItemCancelledEventHandler(
        ILogger<SaleItemCancelledEventHandler> logger,
        IEventStore eventStore,
        IIdempotencyGuard idempotency,
        ISaleReadModelRepository readModel)
        : base(logger, eventStore, idempotency)
    {
        _readModel = readModel;
    }

    protected override async Task ProcessAsync(SaleItemCancelledEvent message, IEventStore eventStore)
    {
        Logger.LogInformation(
            "SaleItemCancelled detail: Number={SaleNumber} ItemId={ItemId} Product={ProductTitle}",
            message.SaleNumber, message.ItemId, message.ProductTitle);

        await eventStore.SaveAsync(message);

        await _readModel.SetItemCancelledAsync(message.SaleId, message.ItemId);
    }
}
