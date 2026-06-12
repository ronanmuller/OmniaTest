using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

public static class CreateCartHandlerTestData
{
    private static readonly Faker<CreateCartItemCommand> ItemFaker = new Faker<CreateCartItemCommand>()
        .RuleFor(i => i.ProductId, _ => Guid.NewGuid())
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 10));

    private static readonly Faker<CreateCartCommand> CommandFaker = new Faker<CreateCartCommand>()
        .RuleFor(c => c.UserId, _ => Guid.NewGuid())
        .RuleFor(c => c.Date, f => f.Date.Recent())
        .RuleFor(c => c.Products, _ => ItemFaker.Generate(2));

    public static CreateCartCommand GenerateValidCommand() => CommandFaker.Generate();
}
