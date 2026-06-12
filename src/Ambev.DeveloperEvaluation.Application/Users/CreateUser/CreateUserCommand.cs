using Ambev.DeveloperEvaluation.Domain.Enums;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.CreateUser;

public class CreateUserCommand : IRequest<CreateUserResult>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public UserRole Role { get; set; }
}