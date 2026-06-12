using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<DeleteProductResponse>;
