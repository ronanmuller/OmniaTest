namespace Ambev.DeveloperEvaluation.Application.Common;

public interface IDomainEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
