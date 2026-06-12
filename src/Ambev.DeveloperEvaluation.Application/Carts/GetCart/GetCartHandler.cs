using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Carts.GetCart;

public class GetCartHandler : IRequestHandler<GetCartQuery, GetCartResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;

    public GetCartHandler(ICartRepository cartRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<GetCartResult> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cart with ID {request.Id} not found");

        return _mapper.Map<GetCartResult>(cart);
    }
}
