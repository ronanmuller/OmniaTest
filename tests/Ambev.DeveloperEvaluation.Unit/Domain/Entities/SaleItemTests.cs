using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleItemTests
{
    [Fact(DisplayName = "Quantities below 4 should have no discount")]
    public void Given_QuantityBelow4_When_Calculated_Then_NoDiscount()
    {
        foreach (var qty in new[] { 1, 2, 3 })
        {
            var item = SaleTestData.GenerateItemWithQuantity(qty);
            item.Calculate();

            item.Discount.Should().Be(0m);
            item.TotalAmount.Should().Be(qty * item.UnitPrice);
        }
    }

    [Fact(DisplayName = "Quantities from 4 to 9 should have 10% discount")]
    public void Given_QuantityBetween4And9_When_Calculated_Then_10PercentDiscount()
    {
        foreach (var qty in new[] { 4, 5, 9 })
        {
            var item = SaleTestData.GenerateItemWithQuantity(qty);
            item.Calculate();

            item.Discount.Should().Be(0.10m);
            item.TotalAmount.Should().Be(qty * item.UnitPrice * 0.90m);
        }
    }

    [Fact(DisplayName = "Quantities from 10 to 20 should have 20% discount")]
    public void Given_QuantityBetween10And20_When_Calculated_Then_20PercentDiscount()
    {
        foreach (var qty in new[] { 10, 15, 20 })
        {
            var item = SaleTestData.GenerateItemWithQuantity(qty);
            item.Calculate();

            item.Discount.Should().Be(0.20m);
            item.TotalAmount.Should().Be(qty * item.UnitPrice * 0.80m);
        }
    }

    [Fact(DisplayName = "Quantity above 20 should throw DomainException")]
    public void Given_QuantityAbove20_When_Calculated_Then_ThrowsDomainException()
    {
        var item = SaleTestData.GenerateItemWithQuantity(21);

        var act = () => item.Calculate();

        act.Should().Throw<DomainException>()
            .WithMessage("*20*");
    }

    [Theory(DisplayName = "TotalAmount = Quantity * UnitPrice * (1 - Discount)")]
    [InlineData(1, 100.00, 0.00, 100.00)]
    [InlineData(4, 50.00, 0.10, 180.00)]
    [InlineData(10, 20.00, 0.20, 160.00)]
    [InlineData(20, 10.00, 0.20, 160.00)]
    public void Given_Item_When_Calculated_Then_TotalIsCorrect(
        int quantity, decimal unitPrice, decimal expectedDiscount, decimal expectedTotal)
    {
        var item = SaleTestData.GenerateItemWithQuantity(quantity);
        item.UnitPrice = unitPrice;

        item.Calculate();

        item.Discount.Should().Be(expectedDiscount);
        item.TotalAmount.Should().Be(expectedTotal);
    }
}
