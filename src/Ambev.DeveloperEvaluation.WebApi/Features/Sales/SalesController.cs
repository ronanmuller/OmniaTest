using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.ResyncReadModel;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseWithData<PaginatedList<ListSalesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSales(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] DateTime? minDate = null,
        [FromQuery] DateTime? maxDate = null,
        [FromQuery] bool? isCancelled = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSalesQuery
        {
            Page = page,
            Size = size,
            Order = order,
            CustomerId = customerId,
            BranchId = branchId,
            MinDate = minDate,
            MaxDate = maxDate,
            IsCancelled = isCancelled
        };
        var result = await _mediator.Send(query, cancellationToken);

        var items = _mapper.Map<List<ListSalesResponse>>(result.Data);
        var paginatedList = new PaginatedList<ListSalesResponse>(items, result.TotalItems, result.CurrentPage, size);

        return OkPaginated(paginatedList);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateSaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateSaleCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedWithData(_mapper.Map<CreateSaleResponse>(result), "Sale created successfully");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleQuery(id), cancellationToken);
        return Ok(_mapper.Map<GetSaleResponse>(result));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSale([FromRoute] Guid id, [FromBody] UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateSaleCommand>(request);
        command.Id = id;

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(_mapper.Map<UpdateSaleResponse>(result));
    }

    [HttpPatch("{id}/items/{itemId}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<CancelSaleItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelSaleItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleItemCommand(id, itemId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("resync-read-model")]
    [ProducesResponseType(typeof(ApiResponseWithData<ResyncReadModelResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResyncReadModel(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResyncReadModelCommand(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSaleCommand(id), cancellationToken);
        return OkWithMessage("Sale cancelled successfully");
    }
}
