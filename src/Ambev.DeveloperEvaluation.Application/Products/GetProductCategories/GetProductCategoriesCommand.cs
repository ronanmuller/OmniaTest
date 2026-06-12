using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProductCategories;

public record GetProductCategoriesQuery : IRequest<IEnumerable<string>>;
