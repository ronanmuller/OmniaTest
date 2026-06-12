using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

public class CreateCartProfile : Profile
{
    public CreateCartProfile()
    {
        CreateMap<CreateCartCommand, Cart>();
        CreateMap<CreateCartItemCommand, CartItem>();
        CreateMap<Cart, CreateCartResult>();
        CreateMap<CartItem, CreateCartItemResult>();
    }
}
