using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

public class CreateCartValidator : AbstractValidator<CreateCartCommand>
{
    public CreateCartValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Date).NotEmpty();
        RuleFor(c => c.Products).NotEmpty().WithMessage("Cart must have at least one product.");
        RuleForEach(c => c.Products).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}
