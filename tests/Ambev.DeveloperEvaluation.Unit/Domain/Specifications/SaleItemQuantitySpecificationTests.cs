using Ambev.DeveloperEvaluation.Domain.Specifications;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Specifications;

public class SaleItemQuantitySpecificationTests
{
    private readonly SaleItemQuantitySpecification _spec = new();

    [Theory(DisplayName = "Given quantity in valid range When checking spec Then returns true")]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(20)]
    public void IsSatisfiedBy_ValidQuantity_ReturnsTrue(int quantity)
    {
        var item = SaleTestData.GenerateItemWithQuantity(quantity);

        _spec.IsSatisfiedBy(item).Should().BeTrue();
    }

    [Theory(DisplayName = "Given quantity outside valid range When checking spec Then returns false")]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(100)]
    public void IsSatisfiedBy_InvalidQuantity_ReturnsFalse(int quantity)
    {
        var item = SaleTestData.GenerateItemWithQuantity(quantity);

        _spec.IsSatisfiedBy(item).Should().BeFalse();
    }
}
