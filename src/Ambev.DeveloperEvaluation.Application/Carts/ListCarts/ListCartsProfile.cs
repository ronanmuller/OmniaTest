using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsProfile : Profile
{
    public ListCartsProfile()
    {
        CreateMap<Cart, ListCartsItemResult>();
        CreateMap<CartItem, ListCartsItemProductResult>();
    }
}
