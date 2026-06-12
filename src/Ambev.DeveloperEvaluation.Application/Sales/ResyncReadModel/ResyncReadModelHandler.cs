using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.ResyncReadModel;

public class ResyncReadModelHandler : IRequestHandler<ResyncReadModelCommand, ResyncReadModelResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleReadModelRepository _readModel;
    private readonly ILogger<ResyncReadModelHandler> _logger;

    public ResyncReadModelHandler(
        ISaleRepository saleRepository,
        ISaleReadModelRepository readModel,
        ILogger<ResyncReadModelHandler> logger)
    {
        _saleRepository = saleRepository;
        _readModel = readModel;
        _logger = logger;
    }

    public async Task<ResyncReadModelResult> Handle(ResyncReadModelCommand request, CancellationToken cancellationToken)
    {
        var synced = 0;
        var failed = 0;
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var (sales, _) = await _saleRepository.GetPagedAsync(page, pageSize, order: null, cancellationToken: cancellationToken);
            if (!sales.Any()) break;

            foreach (var sale in sales)
            {
                try
                {
                    await _readModel.UpsertAsync(ToReadModel(sale), cancellationToken);
                    synced++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "ResyncReadModel: failed to project sale {SaleId}", sale.Id);
                }
            }

            if (sales.Count < pageSize) break;
            page++;
        }

        _logger.LogInformation("ResyncReadModel complete: {Synced} synced, {Failed} failed", synced, failed);
        return new ResyncReadModelResult(synced, failed);
    }

    private static SaleReadModel ToReadModel(Sale sale) => new()
    {
        Id          = sale.Id,
        SaleNumber  = sale.SaleNumber,
        Date        = sale.Date,
        CustomerId  = sale.CustomerId,
        CustomerName = sale.CustomerName,
        BranchId    = sale.BranchId,
        BranchName  = sale.BranchName,
        TotalAmount = sale.TotalAmount,
        IsCancelled = sale.IsCancelled,
        ProjectedAt = DateTime.UtcNow,
        Items = sale.Items.Select(i => new SaleItemReadModel
        {
            Id           = i.Id,
            ProductId    = i.ProductId,
            ProductTitle = i.ProductTitle,
            UnitPrice    = i.UnitPrice,
            Quantity     = i.Quantity,
            Discount     = i.Discount,
            TotalAmount  = i.TotalAmount,
            IsCancelled  = i.IsCancelled
        }).ToList()
    };
}
