using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

public record GetProductQuery(Guid Id) : IRequest<GetProductResult>;
