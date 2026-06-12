using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProductCategories;

public class GetProductCategoriesHandler : IRequestHandler<GetProductCategoriesQuery, IEnumerable<string>>
{
    private readonly IProductRepository _productRepository;

    public GetProductCategoriesHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<string>> Handle(GetProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _productRepository.GetCategoriesAsync(cancellationToken);
    }
}
