using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleProfile : Profile
{
    public UpdateSaleProfile()
    {
        CreateMap<UpdateSaleItemCommand, SaleItem>();
        CreateMap<Sale, UpdateSaleResult>();
        CreateMap<SaleItem, UpdateSaleItemResult>();
    }
}
