namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Stores domain events for guaranteed async delivery (Outbox Pattern).
/// Written in the same DB transaction as the aggregate, processed asynchronously.
/// CorrelationId is stored here (in addition to the JSON payload) for direct SQL querying
/// when troubleshooting a specific request chain.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public Guid CorrelationId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
