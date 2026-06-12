using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsHandler : IRequestHandler<ListCartsQuery, ListCartsResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMapper _mapper;

    public ListCartsHandler(ICartRepository cartRepository, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<ListCartsResult> Handle(ListCartsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _cartRepository.GetPagedAsync(
            request.Page, request.Size, request.Order,
            request.MinDate, request.MaxDate, cancellationToken);

        return new ListCartsResult
        {
            Data = _mapper.Map<List<ListCartsItemResult>>(items),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = (int)Math.Ceiling(total / (double)request.Size)
        };
    }
}
