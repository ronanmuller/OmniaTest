using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Infrastructure;

/// <summary>
/// Base class for all functional tests.
/// Provides a shared factory, HTTP client, JSON serialization settings,
/// and helpers to create users and obtain JWT tokens.
/// </summary>
public abstract class FunctionalTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected FunctionalTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth helpers ──────────────────────────────────────────────────────────

    protected async Task<string> CreateUserAndGetTokenAsync(
        string email = "test@functional.com",
        string password = "Test@123!")
    {
        // Create user (endpoint is public)
        var createResp = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = email.Split('@')[0],
            email,
            password,
            phone = "+5511999999999",
            role = "Manager"
        });

        // 201 or 409 (already exists) — both are fine
        createResp.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Created,
            System.Net.HttpStatusCode.Conflict);

        // Authenticate
        var authResp = await Client.PostAsJsonAsync("/api/Auth", new { email, password });
        authResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await authResp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("token").GetString()!;
    }

    protected void SetBearerToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearBearerToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected async Task<T?> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").Deserialize<T>(JsonOptions);
    }
}
