using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleReadModelRepository _readModel;
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ListSalesHandler> _logger;

    public ListSalesHandler(
        ISaleReadModelRepository readModel,
        ISaleRepository saleRepository,
        IMapper mapper,
        ILogger<ListSalesHandler> logger)
    {
        _readModel = readModel;
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, total) = await _readModel.GetPagedAsync(
                request.Page, request.Size, request.Order,
                request.CustomerId, request.BranchId, request.MinDate, request.MaxDate, request.IsCancelled,
                cancellationToken);

            return new ListSalesResult
            {
                Data = _mapper.Map<List<ListSalesItemResult>>(items),
                TotalItems = total,
                CurrentPage = request.Page,
                TotalPages = (int)Math.Ceiling(total / (double)request.Size)
            };
        }
        catch (Exception ex)
        {
            BusinessMetrics.ReadModelFallbacks.Add(1);
            _logger.LogWarning(ex, "ListSales read model unavailable — falling back to PostgreSQL");
        }

        var (fallbackItems, fallbackTotal) = await _saleRepository.GetPagedAsync(
            request.Page, request.Size, request.Order,
            request.CustomerId, request.BranchId, request.MinDate, request.MaxDate, request.IsCancelled,
            cancellationToken);

        return new ListSalesResult
        {
            Data = _mapper.Map<List<ListSalesItemResult>>(fallbackItems),
            TotalItems = fallbackTotal,
            CurrentPage = request.Page,
            TotalPages = (int)Math.Ceiling(fallbackTotal / (double)request.Size)
        };
    }
}
