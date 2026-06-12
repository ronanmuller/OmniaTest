using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

public class CreateCartHandler : IRequestHandler<CreateCartCommand, CreateCartResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;

    public CreateCartHandler(ICartRepository cartRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<CreateCartResult> Handle(CreateCartCommand command, CancellationToken cancellationToken)
    {
        var cart = _mapper.Map<Cart>(command);
        var created = await _cartRepository.CreateAsync(cart, cancellationToken);
        return _mapper.Map<CreateCartResult>(created);
    }
}
