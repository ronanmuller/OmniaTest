using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

public static class CreateSaleHandlerTestData
{
    private static readonly Faker<CreateSaleItemCommand> ItemFaker = new Faker<CreateSaleItemCommand>()
        .RuleFor(i => i.ProductId, f => Guid.NewGuid())
        .RuleFor(i => i.ProductTitle, f => f.Commerce.ProductName())
        .RuleFor(i => i.UnitPrice, f => Math.Round(f.Random.Decimal(10, 200), 2))
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 3));

    private static readonly Faker<CreateSaleCommand> CommandFaker = new Faker<CreateSaleCommand>()
        .RuleFor(c => c.Date, f => f.Date.Recent())
        .RuleFor(c => c.CustomerId, f => Guid.NewGuid())
        .RuleFor(c => c.CustomerName, f => f.Name.FullName())
        .RuleFor(c => c.BranchId, f => Guid.NewGuid())
        .RuleFor(c => c.BranchName, f => f.Company.CompanyName())
        .RuleFor(c => c.Items, f => ItemFaker.Generate(2));

    public static CreateSaleCommand GenerateValidCommand() => CommandFaker.Generate();
}
