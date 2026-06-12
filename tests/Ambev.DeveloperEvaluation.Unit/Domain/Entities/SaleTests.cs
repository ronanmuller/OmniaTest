using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "SaleNumber is generated on construction")]
    public void Given_NewSale_When_Created_Then_SaleNumberIsGenerated()
    {
        var sale = new Ambev.DeveloperEvaluation.Domain.Entities.Sale();

        sale.SaleNumber.Should().NotBeNullOrEmpty();
        sale.SaleNumber.Should().HaveLength(8);
    }

    [Fact(DisplayName = "Cancel should set IsCancelled to true")]
    public void Given_ActiveSale_When_Cancelled_Then_IsCancelledIsTrue()
    {
        var sale = SaleTestData.GenerateValidSale();

        sale.Cancel();

        sale.IsCancelled.Should().BeTrue();
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "CalculateTotals should sum non-cancelled items")]
    public void Given_SaleWithItems_When_CalculateTotals_Then_TotalIsSumOfItems()
    {
        var sale = SaleTestData.GenerateValidSale(3);
        foreach (var item in sale.Items)
        {
            item.Quantity = 2;
            item.UnitPrice = 100m;
        }

        sale.CalculateTotals();

        sale.TotalAmount.Should().Be(3 * 2 * 100m);
    }

    [Fact(DisplayName = "CalculateTotals should exclude cancelled items")]
    public void Given_SaleWithCancelledItem_When_CalculateTotals_Then_CancelledItemIsExcluded()
    {
        var sale = SaleTestData.GenerateValidSale(2);
        sale.Items[0].Quantity = 1;
        sale.Items[0].UnitPrice = 100m;
        sale.Items[1].Quantity = 1;
        sale.Items[1].UnitPrice = 200m;
        sale.Items[1].IsCancelled = true;

        sale.CalculateTotals();

        sale.TotalAmount.Should().Be(100m);
    }

    [Fact(DisplayName = "CancelItem should mark item as cancelled and update total")]
    public void Given_SaleWithItems_When_ItemCancelled_Then_TotalIsRecalculated()
    {
        var sale = SaleTestData.GenerateValidSale(2);
        sale.Items[0].Quantity = 1;
        sale.Items[0].UnitPrice = 100m;
        sale.Items[0].TotalAmount = 100m;
        sale.Items[1].Quantity = 1;
        sale.Items[1].UnitPrice = 200m;
        sale.Items[1].TotalAmount = 200m;
        sale.TotalAmount = 300m;

        sale.CancelItem(sale.Items[0].Id);

        sale.Items[0].IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(200m);
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "CancelItem throws when item does not exist")]
    public void Given_SaleWithItems_When_CancelNonExistentItem_Then_ThrowsKeyNotFoundException()
    {
        var sale = SaleTestData.GenerateValidSale(1);

        var act = () => sale.CancelItem(Guid.NewGuid());

        act.Should().Throw<KeyNotFoundException>();
    }
}
