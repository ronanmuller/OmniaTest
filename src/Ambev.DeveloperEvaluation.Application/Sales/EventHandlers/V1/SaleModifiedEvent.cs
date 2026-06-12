using Ambev.DeveloperEvaluation.Application.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;

public record SaleModifiedEvent(
    Guid EventId,
    Guid SaleId,
    string SaleNumber,
    decimal TotalAmount,
    Guid CorrelationId,
    Guid CausationId) : IDomainEvent
{
    public string SchemaVersion { get; init; } = "v1";
    public Guid AggregateId => SaleId;
}
