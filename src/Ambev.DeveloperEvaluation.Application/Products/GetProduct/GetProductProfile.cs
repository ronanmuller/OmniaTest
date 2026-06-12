using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

public class GetProductProfile : Profile
{
    public GetProductProfile()
    {
        CreateMap<Product, GetProductResult>()
            .ForMember(dest => dest.Rate, opt => opt.MapFrom(src => src.Rating.Rate))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Rating.Count));
    }
}
