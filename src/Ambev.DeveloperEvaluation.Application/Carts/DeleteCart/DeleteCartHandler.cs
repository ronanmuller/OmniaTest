using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;

public class DeleteCartHandler : IRequestHandler<DeleteCartCommand>
{
    private readonly ICartRepository _cartRepository;

    public DeleteCartHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task Handle(DeleteCartCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _cartRepository.DeleteAsync(request.Id, cancellationToken);
        if (!deleted)
            throw new KeyNotFoundException($"Cart with ID {request.Id} not found");
    }
}
