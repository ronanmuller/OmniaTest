namespace Ambev.DeveloperEvaluation.Application.Common;

/// <summary>
/// Prevents duplicate processing of the same message by the same consumer.
/// Returns true if the message was already processed — handler must skip execution.
/// </summary>
public interface IIdempotencyGuard
{
    Task<bool> IsAlreadyProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default);
}
