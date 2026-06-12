using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, CancelSaleItemResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<CancelSaleItemHandler> _logger;

    public CancelSaleItemHandler(
        ISaleRepository saleRepository,
        IDomainEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationId,
        ILogger<CancelSaleItemHandler> logger)
    {
        _saleRepository = saleRepository;
        _eventPublisher = eventPublisher;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<CancelSaleItemResponse> Handle(CancelSaleItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling item {ItemId} from sale {SaleId}", request.ItemId, request.SaleId);

        var sale = await _saleRepository.GetByIdAsync(request.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID {request.SaleId} not found");

        var saleSpec = new SaleNotCancelledSpecification();
        if (!saleSpec.IsSatisfiedBy(sale))
        {
            _logger.LogWarning("Item cancel rejected: sale {SaleNumber} is cancelled", sale.SaleNumber);
            throw new DomainException($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        }

        var item = sale.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new KeyNotFoundException($"Item {request.ItemId} not found in sale {request.SaleId}");

        var itemSpec = new SaleItemNotCancelledSpecification();
        if (!itemSpec.IsSatisfiedBy(item))
        {
            _logger.LogWarning("Item cancel rejected: item {ProductTitle} is already cancelled", item.ProductTitle);
            throw new DomainException($"Item {item.ProductTitle} is already cancelled");
        }

        sale.CancelItem(request.ItemId);

        var correlationId = _correlationId.CorrelationId;

        await _eventPublisher.PublishAsync(new SaleItemCancelledEvent(
            Guid.NewGuid(),
            sale.Id,
            sale.SaleNumber,
            item.Id,
            item.ProductTitle,
            correlationId,
            CausationId: correlationId), cancellationToken);

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        BusinessMetrics.SaleItemsCancelled.Add(1);

        _logger.LogInformation(
            "Item {ProductTitle} cancelled from sale {SaleNumber}. NewTotal={NewTotal:C}",
            item.ProductTitle, sale.SaleNumber, sale.TotalAmount);

        return new CancelSaleItemResponse
        {
            Success = true,
            NewTotalAmount = sale.TotalAmount
        };
    }
}
