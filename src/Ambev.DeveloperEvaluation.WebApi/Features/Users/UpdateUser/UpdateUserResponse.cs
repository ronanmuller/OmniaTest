using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;

/// <summary>
/// API response model for UpdateUser operation
/// </summary>
public class UpdateUserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UpdateUserNameResponse Name { get; set; } = new();
    public UpdateUserAddressResponse Address { get; set; } = new();
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
}

public class UpdateUserNameResponse
{
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
}

public class UpdateUserAddressResponse
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Zipcode { get; set; } = string.Empty;
    public UpdateUserGeolocationResponse Geolocation { get; set; } = new();
}

public class UpdateUserGeolocationResponse
{
    public string Lat { get; set; } = string.Empty;
    public string Long { get; set; } = string.Empty;
}
