using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

public class ListProductsProfile : Profile
{
    public ListProductsProfile()
    {
        CreateMap<Product, ListProductsItemResult>()
            .ForMember(dest => dest.Rate, opt => opt.MapFrom(src => src.Rating.Rate))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Rating.Count));
    }
}
