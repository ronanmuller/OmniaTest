using Ambev.DeveloperEvaluation.Domain.Specifications;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Specifications;

public class SaleNotCancelledSpecificationTests
{
    private readonly SaleNotCancelledSpecification _spec = new();

    [Fact(DisplayName = "Given active sale When checking spec Then returns true")]
    public void IsSatisfiedBy_ActiveSale_ReturnsTrue()
    {
        var sale = SaleTestData.GenerateValidSale();

        _spec.IsSatisfiedBy(sale).Should().BeTrue();
    }

    [Fact(DisplayName = "Given cancelled sale When checking spec Then returns false")]
    public void IsSatisfiedBy_CancelledSale_ReturnsFalse()
    {
        var sale = SaleTestData.GenerateValidSale();
        sale.Cancel();

        _spec.IsSatisfiedBy(sale).Should().BeFalse();
    }
}
