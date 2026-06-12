using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IDomainEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationId,
        ILogger<CreateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<CreateSaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating sale for customer {CustomerId} at branch {BranchId}",
            command.CustomerId, command.BranchId);

        var sale = new Sale
        {
            SaleNumber = command.SaleNumber,
            Date = command.Date,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName,
            Items = command.Items.Select(i => _mapper.Map<SaleItem>(i)).ToList()
        };

        var quantitySpec = new SaleItemQuantitySpecification();
        foreach (var item in sale.Items)
        {
            if (!quantitySpec.IsSatisfiedBy(item))
            {
                _logger.LogWarning(
                    "Sale creation rejected: item {ProductTitle} has invalid quantity {Quantity}",
                    item.ProductTitle, item.Quantity);
                throw new DomainException(
                    $"Item '{item.ProductTitle}' has invalid quantity {item.Quantity}. Must be between 1 and 20.");
            }
        }

        sale.CalculateTotals();

        var correlationId = _correlationId.CorrelationId;
        var eventId = Guid.NewGuid();

        var itemEvents = sale.Items.Select(i => new SaleCreatedItemEvent(
            i.Id, i.ProductId, i.ProductTitle, i.UnitPrice, i.Quantity, i.Discount, i.TotalAmount)).ToList();

        await _eventPublisher.PublishAsync(new SaleCreatedEvent(
            eventId,
            sale.Id,
            sale.SaleNumber,
            sale.CustomerId,
            sale.CustomerName,
            sale.BranchId,
            sale.BranchName,
            sale.TotalAmount,
            sale.Date,
            itemEvents,
            correlationId,
            CausationId: correlationId), cancellationToken);

        var created = await _saleRepository.CreateAsync(sale, cancellationToken);

        BusinessMetrics.SalesCreated.Add(1);

        _logger.LogInformation("Sale {SaleId} ({SaleNumber}) created. Total={TotalAmount:C}",
            created.Id, created.SaleNumber, created.TotalAmount);

        return _mapper.Map<CreateSaleResult>(created);
    }
}
