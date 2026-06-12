using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

public class SaleCancelledEventHandler : DomainEventHandlerBase<SaleCancelledEvent>
{
    private readonly ISaleReadModelRepository _readModel;

    public SaleCancelledEventHandler(
        ILogger<SaleCancelledEventHandler> logger,
        IEventStore eventStore,
        IIdempotencyGuard idempotency,
        ISaleReadModelRepository readModel)
        : base(logger, eventStore, idempotency)
    {
        _readModel = readModel;
    }

    protected override async Task ProcessAsync(SaleCancelledEvent message, IEventStore eventStore)
    {
        Logger.LogInformation("SaleCancelled detail: Number={SaleNumber}", message.SaleNumber);

        await eventStore.SaveAsync(message);

        // Marca como cancelado no read model sem apagar o documento
        await _readModel.SetCancelledAsync(message.SaleId);
    }
}
