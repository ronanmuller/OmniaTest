using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.ORM.Messaging;

/// <summary>
/// Checks and registers message processing to prevent duplicate handler execution.
/// Uses a composite PK (MessageId + Consumer) — a duplicate insert means already processed.
/// </summary>
public class IdempotencyGuard : IIdempotencyGuard
{
    private readonly DefaultContext _context;
    private readonly ILogger<IdempotencyGuard> _logger;

    public IdempotencyGuard(DefaultContext context, ILogger<IdempotencyGuard> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if this message was already processed by this consumer.
    /// If not, registers it so future calls return true.
    /// </summary>
    public async Task<bool> IsAlreadyProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
    {
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO processed_messages ("MessageId", "Consumer", "ProcessedAt")
            VALUES ({messageId}, {consumer}, {DateTime.UtcNow})
            ON CONFLICT DO NOTHING
            """, ct);

        if (rows == 0)
        {
            _logger.LogWarning(
                "Idempotency: duplicate message {MessageId} for consumer {Consumer} — skipping",
                messageId, consumer);
            return true;
        }

        return false;
    }
}
