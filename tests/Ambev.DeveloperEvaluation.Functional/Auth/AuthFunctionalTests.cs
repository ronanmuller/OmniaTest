using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Ambev.DeveloperEvaluation.Functional.Infrastructure;

namespace Ambev.DeveloperEvaluation.Functional.Auth;

/// <summary>
/// Functional tests for POST /api/Auth.
/// </summary>
public class AuthFunctionalTests : FunctionalTestBase
{
    public AuthFunctionalTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact(DisplayName = "POST /api/Auth: valid credentials return 200 with token")]
    public async Task Authenticate_ValidCredentials_Returns200WithToken()
    {
        // Given — create a user first
        await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "authtest",
            email = "authtest@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "Manager"
        });

        // When
        var response = await Client.PostAsJsonAsync("/api/Auth", new
        {
            email = "authtest@functional.com",
            password = "Test@123!"
        });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetProperty("token").GetString()
            .Should().NotBeNullOrEmpty("a JWT token must be returned on successful auth");
        doc.RootElement.GetProperty("data").GetProperty("email").GetString()
            .Should().Be("authtest@functional.com");
    }

    [Fact(DisplayName = "POST /api/Auth: wrong password returns 401 with JSON body")]
    public async Task Authenticate_WrongPassword_Returns401WithBody()
    {
        // Given
        await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "wrongpwd",
            email = "wrongpwd@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "Customer"
        });

        // When
        var response = await Client.PostAsJsonAsync("/api/Auth", new
        {
            email = "wrongpwd@functional.com",
            password = "SenhaErrada!1"
        });

        // Then — must return 401 with a parseable JSON body (not empty)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeEmpty("401 must always return a JSON body, not an empty response");
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "POST /api/Auth: non-existent user returns 401")]
    public async Task Authenticate_UnknownEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/Auth", new
        {
            email = "naoexiste@functional.com",
            password = "Test@123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /api/Auth: missing email returns 400")]
    public async Task Authenticate_MissingEmail_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/Auth", new { password = "Test@123!" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
