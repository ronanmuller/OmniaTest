using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace Ambev.DeveloperEvaluation.Common.HealthChecks;

public static class HealthChecksExtension
{
    public static void AddBasicHealthChecks(this WebApplicationBuilder builder)
    {
        var hc = builder.Services.AddHealthChecks();

        // Liveness: pure process check — if this fails the process is dead
        hc.AddCheck("Liveness", () => HealthCheckResult.Healthy(), tags: ["liveness"]);

        // PostgreSQL — mandatory, without it the service is not ready
        var postgres = builder.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(postgres))
            hc.AddNpgSql(postgres, name: "PostgreSQL", tags: ["readiness", "database"]);

        // MongoDB — optional (events + read model); degraded when absent, not unhealthy
        var mongo = builder.Configuration.GetConnectionString("MongoConnection");
        if (!string.IsNullOrEmpty(mongo))
            hc.AddMongoDb(
                sp => new MongoDB.Driver.MongoClient(mongo),
                name: "MongoDB",
                failureStatus: HealthStatus.Degraded,
                tags: ["readiness", "mongo"]);

        // Redis — optional (product cache); degraded when absent — circuit breaker handles runtime failures
        var redis = builder.Configuration.GetConnectionString("RedisConnection");
        if (!string.IsNullOrEmpty(redis))
            hc.AddRedis(redis, name: "Redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["readiness", "cache"]);
    }

    public static void UseBasicHealthChecks(this WebApplication app)
    {
        var livenessOptions  = BuildOptions(app, "liveness");
        var readinessOptions = BuildOptions(app, "readiness");
        var allOptions       = BuildOptions(app, null);

        app.UseHealthChecks("/health/live",  livenessOptions);
        app.UseHealthChecks("/health/ready", readinessOptions);
        app.UseHealthChecks("/health",       allOptions);

        var logger = app.Services.GetRequiredService<ILogger<HealthCheckService>>();
        logger.LogInformation("Health checks enabled — /health | /health/live | /health/ready");
    }

    private static HealthCheckOptions BuildOptions(WebApplication app, string? tag)
    {
        return new HealthCheckOptions
        {
            Predicate = tag is null
                ? _ => true
                : check => check.Tags.Contains(tag),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = async (context, report) =>
            {
                var result = new
                {
                    status      = report.Status.ToString(),
                    environment = app.Environment.EnvironmentName.ToLowerInvariant(),
                    checks      = report.Entries.Select(e => new
                    {
                        name        = e.Key,
                        status      = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration    = e.Value.Duration.TotalMilliseconds,
                        error       = e.Value.Exception?.Message
                    })
                };
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsJsonAsync(result);
            }
        };
    }
}
