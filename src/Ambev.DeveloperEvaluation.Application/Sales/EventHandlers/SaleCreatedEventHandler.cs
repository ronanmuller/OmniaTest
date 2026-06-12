using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;


namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

public class SaleCreatedEventHandler : DomainEventHandlerBase<SaleCreatedEvent>
{
    private readonly ISaleReadModelRepository _readModel;

    public SaleCreatedEventHandler(
        ILogger<SaleCreatedEventHandler> logger,
        IEventStore eventStore,
        IIdempotencyGuard idempotency,
        ISaleReadModelRepository readModel)
        : base(logger, eventStore, idempotency)
    {
        _readModel = readModel;
    }

    protected override async Task ProcessAsync(SaleCreatedEvent message, IEventStore eventStore)
    {
        Logger.LogInformation(
            "SaleCreated detail: Number={SaleNumber} Customer={CustomerName} Total={TotalAmount:C}",
            message.SaleNumber, message.CustomerName, message.TotalAmount);

        // Persiste no EventStore (append-only)
        await eventStore.SaveAsync(message);

        // Projeta o read model no MongoDB para leituras futuras
        await _readModel.UpsertAsync(new SaleReadModel
        {
            Id           = message.SaleId,
            SaleNumber   = message.SaleNumber,
            Date         = message.Date,
            CustomerId   = message.CustomerId,
            CustomerName = message.CustomerName,
            BranchId     = message.BranchId,
            BranchName   = message.BranchName,
            TotalAmount  = message.TotalAmount,
            IsCancelled  = false,
            Items        = message.Items.Select(i => new SaleItemReadModel
            {
                Id           = i.ItemId,
                ProductId    = i.ProductId,
                ProductTitle = i.ProductTitle,
                UnitPrice    = i.UnitPrice,
                Quantity     = i.Quantity,
                Discount     = i.Discount,
                TotalAmount  = i.TotalAmount,
                IsCancelled  = false
            }).ToList()
        });
    }
}
