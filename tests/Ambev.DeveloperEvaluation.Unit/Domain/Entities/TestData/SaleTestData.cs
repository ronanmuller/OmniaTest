using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

public static class SaleTestData
{
    private static readonly Faker<SaleItem> ItemFaker = new Faker<SaleItem>()
        .RuleFor(i => i.Id, f => Guid.NewGuid())
        .RuleFor(i => i.ProductId, f => Guid.NewGuid())
        .RuleFor(i => i.ProductTitle, f => f.Commerce.ProductName())
        .RuleFor(i => i.UnitPrice, f => Math.Round(f.Random.Decimal(1, 500), 2));

    public static SaleItem GenerateItemWithQuantity(int quantity)
    {
        var item = ItemFaker.Generate();
        item.Quantity = quantity;
        return item;
    }

    public static Sale GenerateValidSale(int itemCount = 2)
    {
        var faker = new Faker();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            Date = faker.Date.Recent(),
            CustomerId = Guid.NewGuid(),
            CustomerName = faker.Name.FullName(),
            BranchId = Guid.NewGuid(),
            BranchName = faker.Company.CompanyName(),
            Items = Enumerable.Range(1, itemCount)
                .Select(_ => GenerateItemWithQuantity(faker.Random.Int(1, 3)))
                .ToList()
        };
        return sale;
    }
}
