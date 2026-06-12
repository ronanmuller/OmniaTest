using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Specifications;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Specifications;

public class UniqueEmailSpecificationTests
{
    private readonly UniqueEmailSpecification _spec = new();

    [Fact(DisplayName = "Given no existing user When checking email availability Then returns true")]
    public void IsSatisfiedBy_NoExistingUser_ReturnsTrue()
    {
        _spec.IsSatisfiedBy(null).Should().BeTrue();
    }

    [Fact(DisplayName = "Given existing user with same email When checking availability Then returns false")]
    public void IsSatisfiedBy_ExistingUser_ReturnsFalse()
    {
        var existingUser = new User { Email = "taken@example.com" };

        _spec.IsSatisfiedBy(existingUser).Should().BeFalse();
    }
}
