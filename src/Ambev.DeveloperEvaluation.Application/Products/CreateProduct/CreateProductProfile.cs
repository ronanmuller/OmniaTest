using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

public class CreateProductProfile : Profile
{
    public CreateProductProfile()
    {
        CreateMap<CreateProductCommand, Product>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src =>
                new Rating(src.Rate, src.Count)));

        CreateMap<Product, CreateProductResult>()
            .ForMember(dest => dest.Rate, opt => opt.MapFrom(src => src.Rating.Rate))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Rating.Count));
    }
}
