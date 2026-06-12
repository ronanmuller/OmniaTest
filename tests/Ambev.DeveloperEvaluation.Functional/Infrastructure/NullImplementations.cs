using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.WebApi.Services;
using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace Ambev.DeveloperEvaluation.Functional.Infrastructure;

/// <summary>
/// No-op implementations that replace infrastructure dependencies in functional tests.
/// They allow the full DI graph to resolve without any external service running.
/// </summary>

public class NullSaleReadModelRepository : ISaleReadModelRepository
{
    // Always returns null — handler falls back to PostgreSQL (EF InMemory in tests)
    public Task<SaleReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<SaleReadModel?>(null);

    public Task<(IReadOnlyList<SaleReadModel> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        Guid? customerId = null, Guid? branchId = null,
        DateTime? minDate = null, DateTime? maxDate = null,
        bool? isCancelled = null,
        CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<SaleReadModel> Items, int TotalCount)>(([], 0));

    public Task UpsertAsync(SaleReadModel model, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SetCancelledAsync(Guid saleId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SetItemCancelledAsync(Guid saleId, Guid itemId, CancellationToken ct = default)
        => Task.CompletedTask;
}

public class NullEventStore : IEventStore
{
    public Task SaveAsync(string eventType, Guid eventId, Guid aggregateId, object payload,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class NullIdempotencyGuard : IIdempotencyGuard
{
    public Task<bool> IsAlreadyProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
        => Task.FromResult(false);
}

public class NullDomainEventPublisher : IDomainEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;
}

/// <summary>
/// No-op throttle service — always reports no lockout so functional tests
/// are not blocked by the in-memory failure counter shared across test runs.
/// </summary>
public class NullAuthThrottleService : IAuthThrottleService
{
    public bool IsLockedOut(string email, out DateTimeOffset lockedUntil)
    {
        lockedUntil = default;
        return false;
    }
    public void RecordFailure(string email) { }
    public void RecordSuccess(string email) { }
}

public class NullBus : IBus
{
    public Task SendLocal(object commandMessage, IDictionary<string, string>? optionalHeaders = null) => Task.CompletedTask;
    public Task Send(object commandMessage, IDictionary<string, string>? optionalHeaders = null) => Task.CompletedTask;
    public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string>? optionalHeaders = null) => Task.CompletedTask;
    public Task Defer(TimeSpan delay, object message, IDictionary<string, string>? optionalHeaders = null) => Task.CompletedTask;
    public Task Reply(object replyMessage, IDictionary<string, string>? optionalHeaders = null) => Task.CompletedTask;
    public Task Subscribe<TEvent>() => Task.CompletedTask;
    public Task Subscribe(Type eventType) => Task.CompletedTask;
    public Task Unsubscribe<TEvent>() => Task.CompletedTask;
    public Task Unsubscribe(Type eventType) => Task.CompletedTask;
    public Task Publish(object eventMessage, IDictionary<string, string>? optionalHeaders = null) => Task.CompletedTask;
    public IAdvancedApi Advanced => throw new NotSupportedException("NullBus does not support Advanced API");
    public void Dispose() { }
}
