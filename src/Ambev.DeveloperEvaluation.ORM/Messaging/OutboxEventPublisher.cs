using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.ORM.Messaging;

/// <summary>
/// Outbox Pattern implementation of IDomainEventPublisher.
/// Adds the event to the outbox table within the caller's EF transaction — no separate SaveChangesAsync.
/// The command handler's repository save commits both the aggregate and the outbox message atomically.
/// </summary>
public class OutboxEventPublisher : IDomainEventPublisher
{
    private readonly ORM.DefaultContext _context;

    public OutboxEventPublisher(ORM.DefaultContext context)
    {
        _context = context;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var domainEvent = @event as IDomainEvent;

        _context.OutboxMessages.Add(new OutboxMessage
        {
            EventType     = typeof(T).Name,
            EventId       = domainEvent?.EventId ?? Guid.NewGuid(),
            CorrelationId = domainEvent?.CorrelationId ?? Guid.Empty,
            Payload       = JsonSerializer.Serialize(@event)
        });

        // Intentionally no SaveChangesAsync — the caller's repository save commits this atomically.
        return Task.CompletedTask;
    }
}
