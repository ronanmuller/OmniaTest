namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface IEventStore
{
    Task SaveAsync(string eventType, Guid eventId, Guid aggregateId, object payload, CancellationToken cancellationToken = default);
}
