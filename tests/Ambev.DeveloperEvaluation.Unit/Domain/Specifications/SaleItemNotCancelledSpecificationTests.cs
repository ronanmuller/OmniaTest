using Ambev.DeveloperEvaluation.Domain.Specifications;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Specifications;

public class SaleItemNotCancelledSpecificationTests
{
    private readonly SaleItemNotCancelledSpecification _spec = new();

    [Fact(DisplayName = "Given active item When checking spec Then returns true")]
    public void IsSatisfiedBy_ActiveItem_ReturnsTrue()
    {
        var item = SaleTestData.GenerateItemWithQuantity(5);

        _spec.IsSatisfiedBy(item).Should().BeTrue();
    }

    [Fact(DisplayName = "Given cancelled item When checking spec Then returns false")]
    public void IsSatisfiedBy_CancelledItem_ReturnsFalse()
    {
        var item = SaleTestData.GenerateItemWithQuantity(5);
        item.IsCancelled = true;

        _spec.IsSatisfiedBy(item).Should().BeFalse();
    }
}
