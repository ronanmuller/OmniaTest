namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Idempotency guard for Rebus message handlers.
/// A record is inserted before handler logic runs. A duplicate insert means the message
/// was already processed — the handler skips execution entirely.
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
