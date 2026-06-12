using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using FluentAssertions;
using MongoDB.Driver;
using Xunit;

namespace Ambev.DeveloperEvaluation.E2E.Infrastructure;

/// <summary>
/// Base class for E2E tests that require all Docker containers to be running.
///
/// Key differences from FunctionalTestBase:
/// - Uses real MongoDB client to verify the read model projection
/// - WaitForReadModelAsync polls MongoDB until the condition is met or times out
/// - Tests are slower (~10-15s each) because they wait for the async outbox flow
/// </summary>
public abstract class E2ETestBase : IClassFixture<DockerWebApplicationFactory>, IAsyncLifetime
{
    private const string MongoConnectionString = "mongodb://developer:ev%40luAt10n@localhost:27017";
    private const string MongoDatabase = "developer_evaluation_events";
    private const string SaleReadModelsCollection = "sale_read_models";

    protected readonly HttpClient Client;
    protected readonly DockerWebApplicationFactory Factory;
    private readonly IMongoCollection<SaleReadModel> _readModels;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected E2ETestBase(DockerWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        var mongo = new MongoClient(MongoConnectionString);
        var db = mongo.GetDatabase(MongoDatabase);
        _readModels = db.GetCollection<SaleReadModel>(SaleReadModelsCollection);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth helpers ──────────────────────────────────────────────────────────

    protected async Task<string> CreateUserAndGetTokenAsync(
        string email = "e2e@test.com",
        string password = "Test@123!")
    {
        var createResp = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = email.Split('@')[0],
            email,
            password,
            phone = "+5511999999999",
            role = "Manager"
        });

        createResp.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Created,
            System.Net.HttpStatusCode.Conflict);

        var authResp = await Client.PostAsJsonAsync("/api/Auth", new { email, password });
        authResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await authResp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("token").GetString()!;
    }

    protected void SetBearerToken(string token)
        => Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

    // ── MongoDB polling helper ────────────────────────────────────────────────

    /// <summary>
    /// Polls MongoDB until <paramref name="predicate"/> returns true or <paramref name="timeout"/> elapses.
    /// Waits up to 15 seconds by default — enough for the OutboxProcessor (5s interval) +
    /// RabbitMQ delivery + event handler write.
    /// </summary>
    protected async Task<SaleReadModel?> WaitForReadModelAsync(
        Guid saleId,
        Func<SaleReadModel, bool> predicate,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(15));

        while (DateTime.UtcNow < deadline)
        {
            var doc = await _readModels
                .Find(s => s.Id == saleId)
                .FirstOrDefaultAsync();

            if (doc != null && predicate(doc))
                return doc;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return null;
    }
}
