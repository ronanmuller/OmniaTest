using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.ListCarts;

public class ListCartsProfile : Profile
{
    public ListCartsProfile()
    {
        CreateMap<ListCartsItemResult, ListCartsResponse>();
        CreateMap<ListCartsItemProductResult, ListCartsItemResponse>();
    }
}
