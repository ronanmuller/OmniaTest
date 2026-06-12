using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class User : BaseEntity, IUser
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public Address Address { get; set; } = Address.Empty;
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    string IUser.Id => Id.ToString();
    string IUser.Username => Username;
    string IUser.Role => Role.ToString();

    public User()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public ValidationResultDetail Validate()
    {
        var validator = new UserValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }
}
