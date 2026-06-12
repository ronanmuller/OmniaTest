using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;

public record DeleteCartCommand(Guid Id) : IRequest;
