using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

public class SaleModifiedEventHandler : DomainEventHandlerBase<SaleModifiedEvent>
{
    private readonly ISaleReadModelRepository _readModel;

    public SaleModifiedEventHandler(
        ILogger<SaleModifiedEventHandler> logger,
        IEventStore eventStore,
        IIdempotencyGuard idempotency,
        ISaleReadModelRepository readModel)
        : base(logger, eventStore, idempotency)
    {
        _readModel = readModel;
    }

    protected override async Task ProcessAsync(SaleModifiedEvent message, IEventStore eventStore)
    {
        Logger.LogInformation(
            "SaleModified detail: Number={SaleNumber} NewTotal={TotalAmount:C}",
            message.SaleNumber, message.TotalAmount);

        await eventStore.SaveAsync(message);

        // Atualiza apenas o total no read model — os demais campos permanecem
        var existing = await _readModel.GetByIdAsync(message.SaleId);
        if (existing != null)
        {
            existing.TotalAmount = message.TotalAmount;
            await _readModel.UpsertAsync(existing);
        }
    }
}
