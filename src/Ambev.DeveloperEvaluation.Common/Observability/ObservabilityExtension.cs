using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ambev.DeveloperEvaluation.Common.Observability;

/// <summary>
/// Configures OpenTelemetry tracing. Exports to console in development (visible in docker logs).
/// Swap AddConsoleExporter() for AddOtlpExporter() to send to Jaeger / DataDog in production.
/// </summary>
public static class ObservabilityExtension
{
    // Must match DomainActivitySource.Name in Application layer
    private const string DomainSourceName = "Ambev.DeveloperEvaluation";

    public static WebApplicationBuilder AddDefaultObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddSource(DomainSourceName)
                .AddAspNetCoreInstrumentation(opt =>
                {
                    // Propagate X-Correlation-Id as trace attribute
                    opt.EnrichWithHttpRequest = (activity, request) =>
                    {
                        if (request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
                            activity.SetTag("correlation.id", correlationId.ToString());
                    };
                })
                .AddConsoleExporter());

        return builder;
    }
}
