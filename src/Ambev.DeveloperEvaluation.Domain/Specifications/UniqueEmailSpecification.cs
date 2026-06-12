using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Specifications;

/// <summary>
/// Satisfied when no user with the given email already exists.
/// Receives the result of a repository lookup — null means the slot is available.
/// </summary>
public class UniqueEmailSpecification : ISpecification<User?>
{
    public bool IsSatisfiedBy(User? existingUser) => existingUser is null;
}
