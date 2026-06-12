using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

public class ListUsersProfile : Profile
{
    public ListUsersProfile()
    {
        CreateMap<User, ListUsersItemResult>()
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.Address.Number))
            .ForMember(dest => dest.Zipcode, opt => opt.MapFrom(src => src.Address.Zipcode))
            .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Address.Lat))
            .ForMember(dest => dest.Long, opt => opt.MapFrom(src => src.Address.Long));
    }
}
