using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Application.Users.EventHandlers;
using Ambev.DeveloperEvaluation.Application.Users.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Cache;
using Ambev.DeveloperEvaluation.ORM.Messaging;
using Ambev.DeveloperEvaluation.ORM.MongoDB;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Polly;
using Polly.Registry;
using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.Routing.TypeBased;
using StackExchange.Redis;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
        builder.Services.AddScoped<ICartRepository, CartRepository>();

        RegisterMongo(builder);
        RegisterRedis(builder);
        RegisterRebus(builder);
    }

    private static void RegisterMongo(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("MongoConnection");
        if (string.IsNullOrEmpty(connectionString)) return;

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        builder.Services.AddScoped<IEventStore, MongoEventStore>();
        builder.Services.AddScoped<ISaleReadModelRepository, SaleReadModelRepository>();

        RegisterReadModelResiliencePipeline(builder);
    }

    private static void RegisterReadModelResiliencePipeline(WebApplicationBuilder builder)
    {
        // Circuit breaker for MongoDB read model calls.
        // After 3 failures in a 10s window, the circuit opens for 30s —
        // all read attempts return immediately and fall back to PostgreSQL,
        // preventing timeout storms from cascading into the transactional database.
        builder.Services.AddResiliencePipeline<string, SaleReadModel?>(
            ResilienceKeys.SaleReadModel,
            (pipeline, context) =>
            {
                var logger = context.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(InfrastructureModuleInitializer));

                pipeline.AddCircuitBreaker(new()
                {
                    FailureRatio      = 0.5,   // 50% failure rate triggers break
                    MinimumThroughput = 3,     // need at least 3 calls to evaluate
                    SamplingDuration  = TimeSpan.FromSeconds(10),
                    BreakDuration     = TimeSpan.FromSeconds(30),
                    OnOpened  = args =>
                    {
                        logger.LogWarning(
                            "Sale read model circuit opened for {BreakDuration}s — PostgreSQL fallback active",
                            args.BreakDuration.TotalSeconds);
                        return default;
                    },
                    OnClosed  = _ =>
                    {
                        logger.LogInformation("Sale read model circuit closed — MongoDB reads resumed");
                        return default;
                    }
                });
            });
    }

    private static void RegisterRedis(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("RedisConnection");
        if (string.IsNullOrEmpty(connectionString)) return;

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(connectionString));

        RegisterCacheResiliencePipeline(builder);

        builder.Services.AddScoped<ProductRepository>();
        builder.Services.AddScoped<IProductRepository>(provider =>
            new CachedProductRepository(
                provider.GetRequiredService<ProductRepository>(),
                provider.GetRequiredService<IConnectionMultiplexer>(),
                provider.GetRequiredService<ResiliencePipelineProvider<string>>()));
    }

    private static void RegisterCacheResiliencePipeline(WebApplicationBuilder builder)
    {
        // Circuit breaker for Redis cache calls.
        // After 3 failures in a 10s window the circuit opens for 30s —
        // cache reads/writes are skipped and requests fall back to PostgreSQL directly,
        // so a Redis outage never brings down the product endpoints.
        builder.Services.AddResiliencePipeline(
            ResilienceKeys.ProductCache,
            (pipeline, context) =>
            {
                var logger = context.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(InfrastructureModuleInitializer));

                pipeline.AddCircuitBreaker(new()
                {
                    FailureRatio      = 0.5,
                    MinimumThroughput = 3,
                    SamplingDuration  = TimeSpan.FromSeconds(10),
                    BreakDuration     = TimeSpan.FromSeconds(30),
                    OnOpened = args =>
                    {
                        logger.LogWarning(
                            "Product cache circuit opened for {BreakDuration}s — Redis bypassed, serving from PostgreSQL",
                            args.BreakDuration.TotalSeconds);
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("Product cache circuit closed — Redis cache resumed");
                        return default;
                    }
                });
            });
    }

    private static void RegisterRebus(WebApplicationBuilder builder)
    {
        var rabbitMqConnection  = builder.Configuration.GetConnectionString("RabbitMqConnection");
        var postgresConnection  = builder.Configuration.GetConnectionString("DefaultConnection");
        var useRabbitMq         = !string.IsNullOrEmpty(rabbitMqConnection);

        if (!useRabbitMq && string.IsNullOrEmpty(postgresConnection)) return;

        const string inputQueue = "developer-evaluation-queue";

        builder.Services.AddRebus((configure, provider) =>
        {
            // Transport: RabbitMQ when available (production), PostgreSQL as fallback (local/dev).
            // RabbitMQ provides durable queues, dead-letter exchanges and horizontal scale.
            // PostgreSQL transport is sufficient for dev and satisfies the Outbox guarantee alone.
            if (useRabbitMq)
                configure.Transport(t => t.UseRabbitMq(rabbitMqConnection, inputQueue));
            else
                configure.Transport(t => t.UsePostgreSql(postgresConnection!, "rebus_messages", inputQueue));

            return configure
                .Routing(r => r.TypeBased()
                    .Map<SaleCreatedEvent>(inputQueue)
                    .Map<SaleModifiedEvent>(inputQueue)
                    .Map<SaleCancelledEvent>(inputQueue)
                    .Map<SaleItemCancelledEvent>(inputQueue)
                    .Map<UserRegisteredEvent>(inputQueue))
                // Rebus default: 5 delivery attempts, then moves to "<queue>.error" (DLQ).
                // No explicit SimpleRetryStrategy needed — default behavior is correct.
                .Options(o =>
                {
                    o.SetNumberOfWorkers(2);
                    o.SetMaxParallelism(5);
                });
        },
        onCreated: async bus =>
        {
            await bus.Subscribe<SaleCreatedEvent>();
            await bus.Subscribe<SaleModifiedEvent>();
            await bus.Subscribe<SaleCancelledEvent>();
            await bus.Subscribe<SaleItemCancelledEvent>();
            await bus.Subscribe<UserRegisteredEvent>();
        }
        );

        builder.Services.AddRebusHandler<SaleCreatedEventHandler>();
        builder.Services.AddRebusHandler<SaleModifiedEventHandler>();
        builder.Services.AddRebusHandler<SaleCancelledEventHandler>();
        builder.Services.AddRebusHandler<SaleItemCancelledEventHandler>();
        builder.Services.AddRebusHandler<UserRegisteredEventHandler>();

        builder.Services.AddScoped<IDomainEventPublisher, OutboxEventPublisher>();
        builder.Services.AddScoped<IIdempotencyGuard, IdempotencyGuard>();
        builder.Services.AddHostedService<OutboxProcessor>();
    }
}
