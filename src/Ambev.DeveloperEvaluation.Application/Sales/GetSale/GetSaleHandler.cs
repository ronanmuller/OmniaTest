using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ReadModels;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleHandler : IRequestHandler<GetSaleQuery, GetSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleReadModelRepository _readModel;
    private readonly ResiliencePipeline<SaleReadModel?> _pipeline;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSaleHandler> _logger;

    public GetSaleHandler(
        ISaleRepository saleRepository,
        ISaleReadModelRepository readModel,
        ResiliencePipelineProvider<string> pipelineProvider,
        IMapper mapper,
        ILogger<GetSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _readModel = readModel;
        _pipeline = pipelineProvider.GetPipeline<SaleReadModel?>(ResilienceKeys.SaleReadModel);
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GetSaleResult> Handle(GetSaleQuery request, CancellationToken cancellationToken)
    {
        // CQRS Read-side: MongoDB com circuit breaker — se o Mongo falhar repetidamente,
        // o circuito abre e o fallback pro PostgreSQL é imediato (sem esperar timeout).
        SaleReadModel? readModel = null;
        try
        {
            readModel = await _pipeline.ExecuteAsync(
                async ct => await _readModel.GetByIdAsync(request.Id, ct), cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Read model circuit is open for Sale {SaleId} — falling back to PostgreSQL immediately",
                request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Read model unavailable for Sale {SaleId} — falling back to PostgreSQL",
                request.Id);
        }

        if (readModel != null)
            return _mapper.Map<GetSaleResult>(readModel);

        _logger.LogDebug(
            "Sale {SaleId} not in read model — falling back to PostgreSQL (consistency window or circuit open)",
            request.Id);

        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        return _mapper.Map<GetSaleResult>(sale);
    }
}
