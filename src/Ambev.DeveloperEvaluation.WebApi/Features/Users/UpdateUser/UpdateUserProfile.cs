using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;

/// <summary>
/// Profile for mapping between API and Application UpdateUser types
/// </summary>
public class UpdateUserProfile : Profile
{
    public UpdateUserProfile()
    {
        CreateMap<UpdateUserRequest, UpdateUserCommand>()
            .ForMember(dest => dest.Firstname, opt => opt.MapFrom(src => src.Name.Firstname))
            .ForMember(dest => dest.Lastname, opt => opt.MapFrom(src => src.Name.Lastname))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.Address.Number))
            .ForMember(dest => dest.Zipcode, opt => opt.MapFrom(src => src.Address.Zipcode))
            .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Address.Geolocation.Lat))
            .ForMember(dest => dest.Long, opt => opt.MapFrom(src => src.Address.Geolocation.Long));

        CreateMap<UpdateUserResult, UpdateUserResponse>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => new UpdateUserNameResponse
            {
                Firstname = src.Firstname,
                Lastname = src.Lastname
            }))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new UpdateUserAddressResponse
            {
                City = src.City,
                Street = src.Street,
                Number = src.Number,
                Zipcode = src.Zipcode,
                Geolocation = new UpdateUserGeolocationResponse { Lat = src.Lat, Long = src.Long }
            }));
    }
}
