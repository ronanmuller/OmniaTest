using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.Application.Common;

/// <summary>
/// Central ActivitySource for domain operations.
/// Compatible with OpenTelemetry SDK — spans are exported when OTel is configured in the host.
/// Also works standalone: Activity.Current?.TraceId is available in logs even without OTel.
/// </summary>
public static class DomainActivitySource
{
    public const string Name = "Ambev.DeveloperEvaluation";

    private static readonly ActivitySource _source = new(Name, "1.0.0");

    /// <summary>Starts a span for a domain event handler execution.</summary>
    public static Activity? StartEventHandler(
        string handlerName, Guid messageId, string? aggregateId = null,
        Guid correlationId = default, Guid causationId = default)
    {
        var activity = _source.StartActivity($"EventHandler/{handlerName}", ActivityKind.Consumer);
        activity?.SetTag("messaging.message_id", messageId.ToString());
        activity?.SetTag("handler.name", handlerName);
        if (aggregateId is not null)
            activity?.SetTag("aggregate.id", aggregateId);
        if (correlationId != default)
            activity?.SetTag("correlation.id", correlationId.ToString());
        if (causationId != default)
            activity?.SetTag("causation.id", causationId.ToString());
        return activity;
    }

    /// <summary>Marks the current activity as failed and adds exception tags (OTel semantic convention).</summary>
    public static void RecordException(Activity? activity, Exception ex)
    {
        if (activity is null) return;
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        var tags = new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace }
        };
        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }
}
