using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Extension methods for IEventStore.
/// Using a static extension (not a default interface method) ensures NSubstitute
/// can intercept the underlying SaveAsync(string,...) call in tests.
/// </summary>
public static class EventStoreExtensions
{
    /// <summary>Derives EventType and EventId from the IDomainEvent contract — no boilerplate in callers.</summary>
    public static Task SaveAsync<TEvent>(
        this IEventStore eventStore,
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent
        => eventStore.SaveAsync(
            typeof(TEvent).Name,
            @event.EventId,
            @event.AggregateId,
            @event,
            cancellationToken);
}
