using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;

public class ListProductsProfile : Profile
{
    public ListProductsProfile()
    {
        CreateMap<ListProductsItemResult, ListProductsItemResponse>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => new ListProductsRatingResponse
            {
                Rate = src.Rate,
                Count = src.Count
            }));
    }
}
