using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.GetCart;

public record GetCartQuery(Guid Id) : IRequest<GetCartResult>;
