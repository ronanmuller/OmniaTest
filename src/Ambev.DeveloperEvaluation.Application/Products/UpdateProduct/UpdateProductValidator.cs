using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(p => p.Id).NotEmpty();
        RuleFor(p => p.Title).NotEmpty().MaximumLength(200);
        RuleFor(p => p.Price).GreaterThan(0);
        RuleFor(p => p.Category).NotEmpty().MaximumLength(100);
        RuleFor(p => p.Rate).InclusiveBetween(0, 5);
        RuleFor(p => p.Count).GreaterThanOrEqualTo(0);
    }
}
