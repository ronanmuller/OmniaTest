using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;

public class UpdateCartHandler : IRequestHandler<UpdateCartCommand, UpdateCartResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;

    public UpdateCartHandler(ICartRepository cartRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<UpdateCartResult> Handle(UpdateCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cart with ID {command.Id} not found");

        cart.UserId = command.UserId;
        cart.Date = command.Date;
        cart.Products = command.Products.Select(p => new CartItem
        {
            CartId = cart.Id,
            ProductId = p.ProductId,
            Quantity = p.Quantity
        }).ToList();

        var updated = await _cartRepository.UpdateAsync(cart, cancellationToken);
        return _mapper.Map<UpdateCartResult>(updated);
    }
}
