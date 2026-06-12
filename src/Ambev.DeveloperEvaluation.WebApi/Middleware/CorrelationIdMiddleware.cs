using Serilog.Context;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Reads X-Correlation-Id from the request header (or generates one) and pushes it into
/// Serilog's LogContext so every log entry within the request carries CorrelationId automatically.
/// Also writes the value back in the response header so callers can correlate on their side.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
