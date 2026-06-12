using Ambev.DeveloperEvaluation.Application.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;

public record SaleCancelledEvent(
    Guid EventId,
    Guid SaleId,
    string SaleNumber,
    Guid CorrelationId,
    Guid CausationId) : IDomainEvent
{
    public string SchemaVersion { get; init; } = "v1";
    public Guid AggregateId => SaleId;
}
