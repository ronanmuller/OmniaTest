using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

public record ListProductsQuery : IRequest<ListProductsResult>
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
    public string? Order { get; init; }
    public string? Category { get; init; }
}
