using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.ListUsers;

/// <summary>
/// API response model for a single user in the list.
/// </summary>
public class ListUsersItemResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public ListUsersNameResponse Name { get; set; } = new();
    public ListUsersAddressResponse Address { get; set; } = new();
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
}

public class ListUsersNameResponse
{
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
}

public class ListUsersAddressResponse
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Zipcode { get; set; } = string.Empty;
    public ListUsersGeolocationResponse Geolocation { get; set; } = new();
}

public class ListUsersGeolocationResponse
{
    public string Lat { get; set; } = string.Empty;
    public string Long { get; set; } = string.Empty;
}
