using Microsoft.Extensions.Logging;
using Rebus.Handlers;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Common;

/// <summary>
/// Base class for all domain event handlers.
/// Centralizes idempotency, structured logging (MessageId/AggregateId/CorrelationId/CausationId/Status),
/// distributed tracing, and error handling so each handler only implements ProcessAsync().
/// </summary>
public abstract class DomainEventHandlerBase<TEvent> : IHandleMessages<TEvent>
    where TEvent : class, IDomainEvent
{
    protected readonly ILogger Logger;
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyGuard _idempotency;

    protected DomainEventHandlerBase(
        ILogger logger,
        IEventStore eventStore,
        IIdempotencyGuard idempotency)
    {
        Logger = logger;
        _eventStore = eventStore;
        _idempotency = idempotency;
    }

    public async Task Handle(TEvent message)
    {
        var handlerName = GetType().Name;
        var eventName   = typeof(TEvent).Name;

        using var activity = DomainActivitySource.StartEventHandler(
            handlerName,
            message.EventId,
            message.AggregateId.ToString(),
            message.CorrelationId,
            message.CausationId);

        if (await _idempotency.IsAlreadyProcessedAsync(message.EventId, handlerName))
        {
            Logger.LogInformation(
                "{Event} skipped (duplicate): MessageId={MessageId} AggregateId={AggregateId} " +
                "CorrelationId={CorrelationId} Status=skipped",
                eventName, message.EventId, message.AggregateId, message.CorrelationId);
            return;
        }

        Logger.LogInformation(
            "{Event}: MessageId={MessageId} AggregateId={AggregateId} " +
            "CorrelationId={CorrelationId} CausationId={CausationId} Status=processing",
            eventName, message.EventId, message.AggregateId, message.CorrelationId, message.CausationId);

        try
        {
            await ProcessAsync(message, _eventStore);

            Logger.LogInformation(
                "{Event} persisted: MessageId={MessageId} AggregateId={AggregateId} " +
                "CorrelationId={CorrelationId} Status=success",
                eventName, message.EventId, message.AggregateId, message.CorrelationId);
        }
        catch (Exception ex)
        {
            DomainActivitySource.RecordException(activity, ex);
            Logger.LogError(ex,
                "{Event} failed: MessageId={MessageId} AggregateId={AggregateId} " +
                "CorrelationId={CorrelationId} Status=fail",
                eventName, message.EventId, message.AggregateId, message.CorrelationId);
            throw;
        }
    }

    /// <summary>
    /// Implement the domain logic here — idempotency, logging and tracing are handled by the base.
    /// </summary>
    protected abstract Task ProcessAsync(TEvent message, IEventStore eventStore);
}
