using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;

/// <summary>
/// Represents a request to update an existing user.
/// </summary>
public class UpdateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UpdateUserNameRequest Name { get; set; } = new();
    public UpdateUserAddressRequest Address { get; set; } = new();
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
}

public class UpdateUserNameRequest
{
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
}

public class UpdateUserAddressRequest
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Zipcode { get; set; } = string.Empty;
    public UpdateUserGeolocationRequest Geolocation { get; set; } = new();
}

public class UpdateUserGeolocationRequest
{
    public string Lat { get; set; } = string.Empty;
    public string Long { get; set; } = string.Empty;
}
