using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

/// <summary>
/// Result returned for a single user in the list.
/// </summary>
public class ListUsersItemResult
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Zipcode { get; set; } = string.Empty;
    public string Lat { get; set; } = string.Empty;
    public string Long { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
}

/// <summary>
/// Paginated result for the list users operation.
/// </summary>
public class ListUsersResult
{
    public IEnumerable<ListUsersItemResult> Data { get; set; } = [];
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
