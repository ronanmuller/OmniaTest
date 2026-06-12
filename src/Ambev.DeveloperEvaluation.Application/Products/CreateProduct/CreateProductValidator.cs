using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(p => p.Title).NotEmpty().MaximumLength(200);
        RuleFor(p => p.Price).GreaterThan(0);
        RuleFor(p => p.Category).NotEmpty().MaximumLength(100);
        RuleFor(p => p.Rate).InclusiveBetween(0, 5);
        RuleFor(p => p.Count).GreaterThanOrEqualTo(0);
    }
}
