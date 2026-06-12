using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public record ListCartsQuery : IRequest<ListCartsResult>
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
    public string? Order { get; init; }
    public DateTime? MinDate { get; init; }
    public DateTime? MaxDate { get; init; }
}
