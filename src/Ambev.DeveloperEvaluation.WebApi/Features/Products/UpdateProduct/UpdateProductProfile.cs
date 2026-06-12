using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;

public class UpdateProductProfile : Profile
{
    public UpdateProductProfile()
    {
        CreateMap<UpdateProductRequest, UpdateProductCommand>()
            .ForMember(dest => dest.Rate, opt => opt.MapFrom(src => src.Rating.Rate))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Rating.Count))
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UpdateProductResult, UpdateProductResponse>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => new UpdateProductRatingResponse
            {
                Rate = src.Rate,
                Count = src.Count
            }));
    }
}
