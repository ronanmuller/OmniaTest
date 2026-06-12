using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(s => s.CustomerId).NotEmpty();
        RuleFor(s => s.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(s => s.BranchId).NotEmpty();
        RuleFor(s => s.BranchName).NotEmpty().MaximumLength(100);
        RuleFor(s => s.Items).NotEmpty().WithMessage("Sale must have at least one item");
        RuleForEach(s => s.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductTitle).NotEmpty();
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 20)
                .WithMessage("Quantity must be between 1 and 20");
        });
    }
}
