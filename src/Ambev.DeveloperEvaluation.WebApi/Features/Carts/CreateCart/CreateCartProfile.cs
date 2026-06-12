using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.CreateCart;

public class CreateCartProfile : Profile
{
    public CreateCartProfile()
    {
        CreateMap<CreateCartRequest, CreateCartCommand>();
        CreateMap<CreateCartItemRequest, CreateCartItemCommand>();
        CreateMap<CreateCartResult, CreateCartResponse>();
        CreateMap<CreateCartItemResult, CartItemResponse>();
    }
}
