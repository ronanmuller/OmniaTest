namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.ListUsers;

/// <summary>
/// Represents a request to list users with pagination.
/// </summary>
public class ListUsersRequest
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
}
