using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Common.HealthChecks;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.Common.Observability;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using Ambev.DeveloperEvaluation.WebApi.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Threading.RateLimiting;

namespace Ambev.DeveloperEvaluation.WebApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Log.Information("Starting web application");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();
            builder.AddDefaultObservability();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IAuthThrottleService, InMemoryAuthThrottleService>();

            //Somente para demonstrar a funcionalidade (por ip)
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    var response = new ApiResponse
                    {
                        Success = false,
                        Message = "Too many authentication attempts. Please try again later."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                };

                options.AddPolicy("auth", httpContext =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                    opts.JsonSerializerOptions.Converters.Add(new DescriptiveEnumJsonConverterFactory()));
            builder.Services.AddEndpointsApiExplorer();

            builder.AddBasicHealthChecks();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token. Example: eyJhbGci..."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
                )
            );

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.RegisterDependencies();

            builder.Services.AddAutoMapper(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly);

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(ApplicationLayer).Assembly,
                    typeof(Program).Assembly
                );
            });

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            var app = builder.Build();

            // Apply pending migrations automatically on startup so docker-compose up works out of the box.
            // In Testing environment (functional tests) we use EF InMemory which does not support migrations.
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
                if (db.Database.IsRelational())
                    await db.Database.MigrateAsync();
                else
                    await db.Database.EnsureCreatedAsync();
            }

            app.UseDefaultLogging();
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ValidationExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Rate limiting is disabled in the Testing environment because all functional
            // test requests originate from the same loopback IP and would exhaust the
            // per-IP fixed-window limit (10 req/min) after just 10 auth calls.
            if (!app.Environment.IsEnvironment("Testing"))
                app.UseRateLimiter();

            app.UseMiddleware<UnauthorizedResponseMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseBasicHealthChecks();

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
