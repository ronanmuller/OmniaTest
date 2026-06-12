using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ReadModels;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleProfile : Profile
{
    public GetSaleProfile()
    {
        CreateMap<Sale, GetSaleResult>();
        CreateMap<SaleItem, GetSaleItemResult>();

        // CQRS read model → result
        CreateMap<SaleReadModel, GetSaleResult>();
        CreateMap<SaleItemReadModel, GetSaleItemResult>();
    }
}
