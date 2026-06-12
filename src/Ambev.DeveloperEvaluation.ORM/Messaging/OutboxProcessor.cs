using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.ORM.Messaging;

/// <summary>
/// Polls outbox_messages and forwards unprocessed events to the Rebus bus (RabbitMQ or PostgreSQL transport).
/// Rebus handles retry + dead-letter from that point. The outbox itself also tracks publish failures
/// so ops can identify messages that never reached the broker.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private const int MaxPublishAttempts = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan[] RetryBackoff =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2)
    ];

    // Cache: eventType name → CLR type. Built once on first use, never stale within a process lifetime.
    private static readonly Dictionary<string, Type> _typeCache = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
        .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainEvent).IsAssignableFrom(t))
        .ToDictionary(t => t.Name, t => t);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started. Polling every {Interval}s", Interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor: unexpected error during batch processing");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        var bus     = scope.ServiceProvider.GetRequiredService<IBus>();

        var now = DateTime.UtcNow;

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null
                && m.RetryCount < MaxPublishAttempts
                && (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        _logger.LogDebug("OutboxProcessor: processing {Count} messages", messages.Count);

        foreach (var msg in messages)
        {
            try
            {
                var @event = Deserialize(msg.EventType, msg.Payload);

                if (@event is null)
                {
                    // Unknown type — mark as dead immediately rather than retrying forever.
                    msg.RetryCount = MaxPublishAttempts;
                    msg.Error = $"Unknown event type '{msg.EventType}'. Register it as IDomainEvent.";
                    BusinessMetrics.OutboxDeadLetters.Add(1);
                    _logger.LogCritical(
                        "OutboxProcessor: DEAD — unknown EventType '{EventType}' " +
                        "(EventId={EventId} CorrelationId={CorrelationId}). " +
                        "Ensure the event implements IDomainEvent and the assembly is loaded.",
                        msg.EventType, msg.EventId, msg.CorrelationId);
                    continue;
                }

                await bus.Publish(@event);
                msg.ProcessedAt = DateTime.UtcNow;
                msg.NextRetryAt = null;
                msg.Error = null;

                _logger.LogInformation(
                    "OutboxProcessor: published {EventType} (EventId={EventId} CorrelationId={CorrelationId})",
                    msg.EventType, msg.EventId, msg.CorrelationId);
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.Error = ex.Message;
                msg.NextRetryAt = msg.RetryCount >= MaxPublishAttempts
                    ? null
                    : DateTime.UtcNow.Add(GetBackoffDelay(msg.RetryCount));

                if (msg.RetryCount >= MaxPublishAttempts)
                {
                    BusinessMetrics.OutboxDeadLetters.Add(1);
                    _logger.LogCritical(ex,
                        "OutboxProcessor: DEAD — {EventType} " +
                        "(EventId={EventId} CorrelationId={CorrelationId}) failed {Attempts} times. " +
                        "Manual intervention required.",
                        msg.EventType, msg.EventId, msg.CorrelationId, MaxPublishAttempts);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "OutboxProcessor: publish failed for {EventType} " +
                        "(EventId={EventId} CorrelationId={CorrelationId}). " +
                        "Attempt {Attempt}/{Max}. Next retry at {NextRetryAt} UTC.",
                        msg.EventType, msg.EventId, msg.CorrelationId, msg.RetryCount, MaxPublishAttempts, msg.NextRetryAt);
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Resolves the CLR type from the event name using the IDomainEvent type cache,
    /// then deserializes the JSON payload. No manual switch needed — new events are
    /// picked up automatically as long as they implement IDomainEvent.
    /// </summary>
    private static TimeSpan GetBackoffDelay(int retryCount)
    {
        var index = Math.Clamp(retryCount - 1, 0, RetryBackoff.Length - 1);
        return RetryBackoff[index];
    }

    private static object? Deserialize(string eventType, string payload)
    {
        if (!_typeCache.TryGetValue(eventType, out var type))
            return null;

        return JsonSerializer.Deserialize(payload, type);
    }
}
