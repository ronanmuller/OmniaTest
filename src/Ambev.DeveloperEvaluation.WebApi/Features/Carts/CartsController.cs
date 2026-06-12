using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Carts.CreateCart;
using Ambev.DeveloperEvaluation.WebApi.Features.Carts.GetCart;
using Ambev.DeveloperEvaluation.WebApi.Features.Carts.UpdateCart;
using Ambev.DeveloperEvaluation.WebApi.Features.Carts.ListCarts;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Application.Carts.GetCart;
using Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;
using Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;
using Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CartsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public CartsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseWithData<PaginatedList<ListCartsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCarts(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        [FromQuery(Name = "_minDate")] DateTime? minDate = null,
        [FromQuery(Name = "_maxDate")] DateTime? maxDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListCartsQuery { Page = page, Size = size, Order = order, MinDate = minDate, MaxDate = maxDate };
        var result = await _mediator.Send(query, cancellationToken);

        var items = _mapper.Map<List<ListCartsResponse>>(result.Data);
        var paginatedList = new PaginatedList<ListCartsResponse>(items, result.TotalItems, result.CurrentPage, size);

        return OkPaginated(paginatedList);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateCartResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateCartCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedWithData(_mapper.Map<CreateCartResponse>(result), "Cart created successfully");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetCartResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCart([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCartQuery(id), cancellationToken);
        return Ok(_mapper.Map<GetCartResponse>(result));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateCartResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCart([FromRoute] Guid id, [FromBody] UpdateCartRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateCartCommand>(request);
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(_mapper.Map<UpdateCartResponse>(result));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCart([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCartCommand(id), cancellationToken);
        return OkWithMessage("Cart deleted successfully");
    }
}
