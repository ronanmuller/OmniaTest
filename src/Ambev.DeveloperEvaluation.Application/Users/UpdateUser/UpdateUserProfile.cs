using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

public class UpdateUserProfile : Profile
{
    public UpdateUserProfile()
    {
        CreateMap<UpdateUserCommand, User>()
            .ForMember(dest => dest.Password, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src =>
                new Address(src.City, src.Street, src.Number, src.Zipcode, src.Lat, src.Long)));

        CreateMap<User, UpdateUserResult>()
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.Address.Number))
            .ForMember(dest => dest.Zipcode, opt => opt.MapFrom(src => src.Address.Zipcode))
            .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Address.Lat))
            .ForMember(dest => dest.Long, opt => opt.MapFrom(src => src.Address.Long));
    }
}
