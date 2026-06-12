using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Templates;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.Common.Logging;



/// <summary> Add default Logging configuration to project. This configuration supports Serilog logs with DataDog compatible output.</summary>
public static class LoggingExtension
{
    /// <summary>
    /// The destructuring options builder configured with default destructurers and a custom DbUpdateExceptionDestructurer.
    /// </summary>
    static readonly DestructuringOptionsBuilder _destructuringOptionsBuilder = new DestructuringOptionsBuilder()
        .WithDefaultDestructurers()
        .WithDestructurers([new DbUpdateExceptionDestructurer()]);

    /// <summary>
    /// A filter predicate to exclude log events with specific criteria.
    /// </summary>
    static readonly Func<LogEvent, bool> _filterPredicate = exclusionPredicate =>
    {
        if (exclusionPredicate.Level != LogEventLevel.Information) return false;

        exclusionPredicate.Properties.TryGetValue("StatusCode", out var statusCode);
        exclusionPredicate.Properties.TryGetValue("Path", out var path);

        var isHealthPath = path?.ToString().Contains("/health") ?? false;
        var is200 = statusCode?.ToString().Equals("200") ?? false;

        return isHealthPath && is200;
    };

    /// <summary>
    /// This method configures the logging with commonly used features for DataDog integration.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> to add services to.</param>
    /// <returns>A <see cref="WebApplicationBuilder"/> that can be used to further configure the API services.</returns>
    /// <remarks>
    /// <para>Logging output are diferents on Debug and Release modes.</para>
    /// </remarks> 
    public static WebApplicationBuilder AddDefaultLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails(_destructuringOptionsBuilder)
                .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
                .Filter.ByExcluding(_filterPredicate);

            // Template includes CorrelationId and TraceId for full observability
            const string debugTemplate  = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {CorrelationId} {TraceId} {Message:lj}{NewLine}{Exception}";
            const string releaseTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} CorrelationId={CorrelationId} TraceId={TraceId} {Message:lj}{NewLine}{Exception}";

            if (Debugger.IsAttached)
            {
                loggerConfiguration.Enrich.WithProperty("DebuggerAttached", Debugger.IsAttached);
                loggerConfiguration.WriteTo.Console(outputTemplate: debugTemplate, theme: SystemConsoleTheme.Colored);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console(outputTemplate: releaseTemplate)
                    .WriteTo.File(
                        "logs/log-.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: releaseTemplate);
            }
        });

        builder.Services.AddLogging();

        return builder;
    }

    /// <summary>Adds middleware for Swagger documetation generation.</summary>
    /// <param name="app">The <see cref="WebApplication"/> instance this method extends.</param>
    /// <returns>The <see cref="WebApplication"/> for Swagger documentation.</returns>
    public static WebApplication UseDefaultLogging(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Logger>>();

        var mode = Debugger.IsAttached ? "Debug" : "Release";
        logger.LogInformation("Logging enabled for '{Application}' on '{Environment}' - Mode: {Mode}", app.Environment.ApplicationName, app.Environment.EnvironmentName, mode);
        return app;

    }
}
