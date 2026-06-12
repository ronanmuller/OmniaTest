using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.GetProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.GetProductCategories;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ProductsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseWithData<PaginatedList<ListProductsItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProducts(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductsQuery { Page = page, Size = size, Order = order, Category = category };
        var result = await _mediator.Send(query, cancellationToken);

        var items = _mapper.Map<List<ListProductsItemResponse>>(result.Data);
        var paginatedList = new PaginatedList<ListProductsItemResponse>(items, result.TotalItems, result.CurrentPage, size);

        return OkPaginated(paginatedList);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponseWithData<IEnumerable<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new GetProductCategoriesQuery(), cancellationToken);
        return Ok(categories);
    }

    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(ApiResponseWithData<PaginatedList<ListProductsItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByCategory(
        [FromRoute] string category,
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductsQuery { Page = page, Size = size, Order = order, Category = category };
        var result = await _mediator.Send(query, cancellationToken);

        var items = _mapper.Map<List<ListProductsItemResponse>>(result.Data);
        var paginatedList = new PaginatedList<ListProductsItemResponse>(items, result.TotalItems, result.CurrentPage, size);

        return OkPaginated(paginatedList);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateProductCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedWithData(_mapper.Map<CreateProductResponse>(result), "Product created successfully");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductQuery(id), cancellationToken);
        return Ok(_mapper.Map<GetProductResponse>(result));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateProductCommand>(request);
        command.Id = id;

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(_mapper.Map<UpdateProductResponse>(result));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return OkWithMessage("Product deleted successfully");
    }
}
