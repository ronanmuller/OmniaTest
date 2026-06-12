using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

public class UpdateProductProfile : Profile
{
    public UpdateProductProfile()
    {
        CreateMap<UpdateProductCommand, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src =>
                new Rating(src.Rate, src.Count)));

        CreateMap<Product, UpdateProductResult>()
            .ForMember(dest => dest.Rate, opt => opt.MapFrom(src => src.Rating.Rate))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Rating.Count));
    }
}
