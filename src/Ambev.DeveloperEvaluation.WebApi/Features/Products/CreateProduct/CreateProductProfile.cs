using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;

public class CreateProductProfile : Profile
{
    public CreateProductProfile()
    {
        CreateMap<CreateProductRequest, CreateProductCommand>()
            .ForMember(dest => dest.Rate, opt => opt.MapFrom(src => src.Rating.Rate))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Rating.Count));

        CreateMap<CreateProductResult, CreateProductResponse>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => new ProductRatingResponse
            {
                Rate = src.Rate,
                Count = src.Count
            }));
    }
}
