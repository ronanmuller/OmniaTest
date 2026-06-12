using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Ambev.DeveloperEvaluation.Functional.Infrastructure;

namespace Ambev.DeveloperEvaluation.Functional.Users;

/// <summary>
/// Functional tests for /api/Users.
///
/// Errors caught historically:
/// - CreateUser returned empty name/email/role/status (CreateUserResult only had Id)
/// - 'Status deve ser diferente de Unknown' — AutoMapper mapped Unknown as default
/// - Role enum validation gave unhelpful error (missing accepted values list)
/// </summary>
public class UsersFunctionalTests : FunctionalTestBase
{
    public UsersFunctionalTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact(DisplayName = "POST /api/Users: valid payload returns 201 with all fields populated")]
    public async Task CreateUser_ValidPayload_Returns201WithAllFields()
    {
        // When
        var response = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "joaofunc",
            email = "joaofunc@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "Manager"
        });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");

        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("name").GetString().Should().Be("joaofunc",
            "name must be populated — historically was empty due to missing ForMember mapping");
        data.GetProperty("email").GetString().Should().Be("joaofunc@functional.com",
            "email must be populated — historically was empty");
        data.GetProperty("role").GetString().Should().Be("Manager",
            "role must reflect what was sent — historically returned 'None'");
        data.GetProperty("status").GetString().Should().Be("Active",
            "status must default to Active — historically returned 'Unknown'");
    }

    [Fact(DisplayName = "POST /api/Users: duplicate email returns 409")]
    public async Task CreateUser_DuplicateEmail_Returns409()
    {
        var payload = new
        {
            username = "dupeuser",
            email = "dupeuser@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "Customer"
        };

        await Client.PostAsJsonAsync("/api/Users", payload);
        var response = await Client.PostAsJsonAsync("/api/Users", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "POST /api/Users: invalid role returns 400 with accepted values in message")]
    public async Task CreateUser_InvalidRole_Returns400WithHelpfulMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "badRole",
            email = "badrole@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "INVALIDO"
        });

        // Then — historically this was a cryptic 500; now must be 400 with accepted values
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().ContainAny("None", "Customer", "Manager", "Admin",
            "error message must list accepted enum values");
    }

    [Fact(DisplayName = "POST /api/Users: missing password returns 400")]
    public async Task CreateUser_MissingPassword_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "nopwd",
            email = "nopwd@functional.com",
            phone = "+5511999999999",
            role = "Customer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /api/Users/{id}: unauthenticated returns 401 with JSON body")]
    public async Task GetUser_Unauthenticated_Returns401WithBody()
    {
        ClearBearerToken();

        var response = await Client.GetAsync($"/api/Users/{Guid.NewGuid()}");

        // Then — historically returned 401 with empty body (middleware order bug)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeEmpty("401 must return a JSON body, not an empty response");
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "GET /api/Users/{id}: authenticated returns 200 with user data")]
    public async Task GetUser_Authenticated_Returns200()
    {
        // Given — create user and capture their ID from the create response
        var createResp = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "getusertest",
            email = "getuser@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "Manager"
        });
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var userId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        var token = await CreateUserAndGetTokenAsync("getuser@functional.com");
        SetBearerToken(token);

        // When
        var response = await Client.GetAsync($"/api/Users/{userId}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /api/Users/{id}: unknown id returns 404")]
    public async Task GetUser_UnknownId_Returns404()
    {
        var token = await CreateUserAndGetTokenAsync("notfound@functional.com");
        SetBearerToken(token);

        var response = await Client.GetAsync($"/api/Users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /api/Users/{id}: authenticated owner can delete")]
    public async Task DeleteUser_AuthenticatedOwner_Returns200()
    {
        // Create user as Admin (DELETE /api/Users requires Admin role)
        var createResp = await Client.PostAsJsonAsync("/api/Users", new
        {
            username = "deleteme",
            email = "deleteuser@functional.com",
            password = "Test@123!",
            phone = "+5511999999999",
            role = "Admin"
        });
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var userId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        var token = await CreateUserAndGetTokenAsync("deleteuser@functional.com");
        SetBearerToken(token);

        var response = await Client.DeleteAsync($"/api/Users/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
