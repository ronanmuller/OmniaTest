using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleProfile : Profile
{
    public CreateSaleProfile()
    {
        CreateMap<CreateSaleItemCommand, SaleItem>();
        CreateMap<Sale, CreateSaleResult>();
        CreateMap<SaleItem, CreateSaleItemResult>();
    }
}
