using System.Diagnostics.Metrics;

namespace Ambev.DeveloperEvaluation.Application.Common;

/// <summary>
/// OTel-native business metrics via System.Diagnostics.Metrics.
/// Counters are exported automatically when the OTel SDK meter is registered.
/// These metrics answer "is the business working?" — separate from technical health checks.
/// </summary>
public static class BusinessMetrics
{
    public const string MeterName = "Ambev.DeveloperEvaluation";

    private static readonly Meter _meter = new(MeterName, "1.0.0");

    /// <summary>Number of sales successfully created.</summary>
    public static readonly Counter<long> SalesCreated =
        _meter.CreateCounter<long>("sales.created", "sales", "Number of sales created");

    /// <summary>Number of sales cancelled (DELETE /api/sales/{id}).</summary>
    public static readonly Counter<long> SalesCancelled =
        _meter.CreateCounter<long>("sales.cancelled", "sales", "Number of sales cancelled");

    /// <summary>Number of individual sale items cancelled.</summary>
    public static readonly Counter<long> SaleItemsCancelled =
        _meter.CreateCounter<long>("sales.items.cancelled", "items", "Number of sale items cancelled");

    /// <summary>
    /// Number of times the MongoDB read model was unavailable and the system fell back to PostgreSQL.
    /// High values indicate MongoDB instability — investigate the circuit breaker state.
    /// </summary>
    public static readonly Counter<long> ReadModelFallbacks =
        _meter.CreateCounter<long>("read_model.fallbacks", "requests",
            "Times MongoDB was unavailable and PostgreSQL fallback was used");

    /// <summary>
    /// Number of outbox messages that exceeded the retry limit and were marked dead.
    /// Any value above zero requires manual intervention.
    /// </summary>
    public static readonly Counter<long> OutboxDeadLetters =
        _meter.CreateCounter<long>("outbox.dead_letters", "messages",
            "Outbox messages that exceeded retry limit and require manual intervention");
}
