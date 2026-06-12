using Ambev.DeveloperEvaluation.Application.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;

public record SaleCreatedEvent(
    Guid EventId,
    Guid SaleId,
    string SaleNumber,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    DateTime Date,
    IReadOnlyList<SaleCreatedItemEvent> Items,
    Guid CorrelationId,
    Guid CausationId) : IDomainEvent
{
    public string SchemaVersion { get; init; } = "v1";
    public Guid AggregateId => SaleId;
}

public record SaleCreatedItemEvent(
    Guid ItemId,
    Guid ProductId,
    string ProductTitle,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    decimal TotalAmount);
