using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public UpdateProductHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(command.Id, cancellationToken);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {command.Id} not found");

        _mapper.Map(command, product);
        product.UpdatedAt = DateTime.UtcNow;

        var updated = await _productRepository.UpdateAsync(product, cancellationToken);
        return _mapper.Map<UpdateProductResult>(updated);
    }
}
