namespace Ambev.DeveloperEvaluation.Domain.Common;

/// <summary>
/// Marker interface for all versioned domain events.
/// Provides the fields required for idempotency, structured logging, distributed tracing,
/// and causation tracking across asynchronous boundaries.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    Guid AggregateId { get; }

    /// <summary>
    /// Ties every event in a request/workflow to a single root cause.
    /// Set from X-Correlation-Id on the HTTP boundary; propagated unchanged to all
    /// downstream events so you can grep the entire chain with one ID.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Points to the EventId (or HTTP request ID) that directly caused this event.
    /// Together with CorrelationId this reconstructs the full causation tree:
    ///   Request → SaleCreated → StockReserved → InvoiceGenerated
    ///   (all share CorrelationId; each CausationId points to its parent EventId)
    /// </summary>
    Guid CausationId { get; }
}
