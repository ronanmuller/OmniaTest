using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ambev.DeveloperEvaluation.E2E.Infrastructure;

/// <summary>
/// Boots the real ASP.NET Core pipeline using the Development environment,
/// which picks up appsettings.Development.json with connection strings pointing
/// to the local Docker containers (PostgreSQL, MongoDB, RabbitMQ, Redis).
///
/// Unlike CustomWebApplicationFactory (functional tests), this factory makes NO
/// substitutions — every piece of infrastructure is real. This means:
/// - OutboxProcessor runs and publishes events to RabbitMQ
/// - SaleCreatedEventHandler consumes and writes to MongoDB
/// - The full async flow is exercised
///
/// Requires: docker-compose up (all containers healthy) before running tests.
/// </summary>
public class DockerWebApplicationFactory : WebApplicationFactory<Ambev.DeveloperEvaluation.WebApi.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development env = appsettings.Development.json = real Docker connection strings
        builder.UseEnvironment("Development");
    }
}
