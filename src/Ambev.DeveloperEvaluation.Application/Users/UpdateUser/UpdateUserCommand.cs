using Ambev.DeveloperEvaluation.Domain.Enums;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

public class UpdateUserCommand : IRequest<UpdateUserResult>
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
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
