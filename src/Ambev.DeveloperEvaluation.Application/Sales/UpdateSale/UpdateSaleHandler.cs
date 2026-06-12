using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<UpdateSaleHandler> _logger;

    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IDomainEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationId,
        ILogger<UpdateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating sale {SaleId}", command.Id);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        var notCancelled = new SaleNotCancelledSpecification();
        if (!notCancelled.IsSatisfiedBy(sale))
        {
            _logger.LogWarning("Update rejected: sale {SaleNumber} is cancelled", sale.SaleNumber);
            throw new DomainException($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        }

        sale.Date = command.Date;
        sale.CustomerId = command.CustomerId;
        sale.CustomerName = command.CustomerName;
        sale.BranchId = command.BranchId;
        sale.BranchName = command.BranchName;
        sale.Items = command.Items.Select(i =>
        {
            var item = _mapper.Map<SaleItem>(i);
            item.SaleId = sale.Id;
            return item;
        }).ToList();
        sale.UpdatedAt = DateTime.UtcNow;

        var quantitySpec = new SaleItemQuantitySpecification();
        foreach (var item in sale.Items)
        {
            if (!quantitySpec.IsSatisfiedBy(item))
            {
                _logger.LogWarning(
                    "Update rejected: item {ProductTitle} has invalid quantity {Quantity}",
                    item.ProductTitle, item.Quantity);
                throw new DomainException(
                    $"Item '{item.ProductTitle}' has invalid quantity {item.Quantity}. Must be between 1 and 20.");
            }
        }

        sale.CalculateTotals();

        var correlationId = _correlationId.CorrelationId;

        await _eventPublisher.PublishAsync(new SaleModifiedEvent(
            Guid.NewGuid(),
            sale.Id,
            sale.SaleNumber,
            sale.TotalAmount,
            correlationId,
            CausationId: correlationId), cancellationToken);

        var updated = await _saleRepository.UpdateAsync(sale, cancellationToken);

        _logger.LogInformation("Sale {SaleId} ({SaleNumber}) updated. NewTotal={TotalAmount:C}",
            updated.Id, updated.SaleNumber, updated.TotalAmount);

        return _mapper.Map<UpdateSaleResult>(updated);
    }
}
