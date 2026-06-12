using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.Functional.Infrastructure;

/// <summary>
/// Boots the real ASP.NET Core pipeline (middlewares, DI, AutoMapper, MediatR)
/// but replaces external dependencies that require running infrastructure:
/// - PostgreSQL → EF InMemory (isolated per test class via unique DB name)
/// - RabbitMQ / Rebus → removed (no message bus in tests)
/// - MongoDB → NullSaleReadModelRepository (read model always returns null → fallback to EF)
/// - Redis → removed (cache disabled)
///
/// This means every test exercises the full HTTP stack — routing, auth middleware,
/// validation pipeline, AutoMapper profiles, MediatR handlers — with zero docker dependency.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Ambev.DeveloperEvaluation.WebApi.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ── Replace PostgreSQL with EF InMemory ──────────────────────────
            services.RemoveAll<DbContextOptions<DefaultContext>>();
            services.RemoveAll<DefaultContext>();

            services.AddDbContext<DefaultContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // ── Remove Rebus / RabbitMQ ──────────────────────────────────────
            // Rebus tries to connect to RabbitMQ on startup — remove it entirely.
            // Domain events are not tested here; that's covered by unit tests.
            var rebusDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Rebus") == true ||
                            d.ImplementationType?.FullName?.Contains("Rebus") == true ||
                            d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("Rebus") == true)
                .ToList();
            foreach (var d in rebusDescriptors)
                services.Remove(d);

            // Remove IBus in case any handler depends on it
            services.RemoveAll<IBus>();
            services.AddSingleton<IBus, NullBus>();

            // ── Remove Redis cache ────────────────────────────────────────────
            var redisDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Redis") == true ||
                            d.ServiceType.FullName?.Contains("StackExchange") == true ||
                            d.ImplementationType?.FullName?.Contains("Redis") == true ||
                            d.ImplementationType?.FullName?.Contains("StackExchange") == true)
                .ToList();
            foreach (var d in redisDescriptors)
                services.Remove(d);

            // ── Remove MongoDB (read model falls back to EF) ─────────────────
            var mongoDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Mongo") == true ||
                            d.ImplementationType?.FullName?.Contains("Mongo") == true)
                .ToList();
            foreach (var d in mongoDescriptors)
                services.Remove(d);

            // Register null implementations so DI doesn't fail on missing registrations
            services.AddScoped<Domain.Repositories.ISaleReadModelRepository, NullSaleReadModelRepository>();
            services.AddScoped<Domain.Repositories.IEventStore, NullEventStore>();
            services.AddScoped<Application.Common.IIdempotencyGuard, NullIdempotencyGuard>();
            services.AddScoped<Application.Common.IDomainEventPublisher, NullDomainEventPublisher>();

            // Register no-op resilience pipelines so handlers that use Polly don't throw
            // InvalidOperationException (which maps to 409) when pipelines are not registered.
            services.AddResiliencePipeline<string, SaleReadModel?>(
                ResilienceKeys.SaleReadModel, _ => { });
            services.AddResiliencePipeline(
                ResilienceKeys.ProductCache, _ => { });

            // Remove OutboxProcessor (hosted service that needs RabbitMQ)
            var outboxDescriptors = services
                .Where(d => d.ImplementationType?.Name?.Contains("OutboxProcessor") == true)
                .ToList();
            foreach (var d in outboxDescriptors)
                services.Remove(d);

            // ── Disable auth throttle in tests ───────────────────────────────
            // InMemoryAuthThrottleService is a singleton — failure counts would bleed
            // between tests if any test exercises the wrong-password path.
            // Rate limiting itself is disabled via Program.cs (UseRateLimiter skipped
            // in the Testing environment).
            services.RemoveAll<IAuthThrottleService>();
            services.AddSingleton<IAuthThrottleService, NullAuthThrottleService>();

            // ── Ensure DB is created ──────────────────────────────────────────
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            db.Database.EnsureCreated();
        });
    }
}
