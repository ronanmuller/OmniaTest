using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

public class ListProductsHandler : IRequestHandler<ListProductsQuery, ListProductsResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public ListProductsHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ListProductsResult> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _productRepository.GetPagedAsync(
            request.Page, request.Size, request.Order, request.Category, cancellationToken);

        return new ListProductsResult
        {
            Data = _mapper.Map<List<ListProductsItemResult>>(items),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = (int)Math.Ceiling(total / (double)request.Size)
        };
    }
}
