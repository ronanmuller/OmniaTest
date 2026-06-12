using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.GetProduct;

public class GetProductProfile : Profile
{
    public GetProductProfile()
    {
        CreateMap<GetProductResult, GetProductResponse>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => new GetProductRatingResponse
            {
                Rate = src.Rate,
                Count = src.Count
            }));
    }
}
