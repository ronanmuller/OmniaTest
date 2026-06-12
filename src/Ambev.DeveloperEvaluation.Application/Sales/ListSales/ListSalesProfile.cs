using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ReadModels;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<Sale, ListSalesItemResult>();

        // CQRS read model → result
        CreateMap<SaleReadModel, ListSalesItemResult>();
    }
}
