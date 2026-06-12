using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

public class DeleteSaleHandler : IRequestHandler<DeleteSaleCommand, DeleteSaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<DeleteSaleHandler> _logger;

    public DeleteSaleHandler(
        ISaleRepository saleRepository,
        IDomainEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationId,
        ILogger<DeleteSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _eventPublisher = eventPublisher;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<DeleteSaleResponse> Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling sale {SaleId}", request.Id);

        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        var spec = new SaleNotCancelledSpecification();
        if (!spec.IsSatisfiedBy(sale))
        {
            _logger.LogWarning("Cancel rejected: sale {SaleNumber} is already cancelled", sale.SaleNumber);
            throw new DomainException($"Sale {sale.SaleNumber} is already cancelled");
        }

        sale.Cancel();

        var correlationId = _correlationId.CorrelationId;

        await _eventPublisher.PublishAsync(new SaleCancelledEvent(
            Guid.NewGuid(),
            sale.Id,
            sale.SaleNumber,
            correlationId,
            CausationId: correlationId), cancellationToken);

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        BusinessMetrics.SalesCancelled.Add(1);

        _logger.LogInformation("Sale {SaleId} ({SaleNumber}) cancelled", sale.Id, sale.SaleNumber);

        return new DeleteSaleResponse { Success = true };
    }
}
