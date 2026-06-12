using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Users.ListUsers;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.ListUsers;

/// <summary>
/// Profile for mapping between Application and API ListUsers types
/// </summary>
public class ListUsersProfile : Profile
{
    public ListUsersProfile()
    {
        CreateMap<ListUsersRequest, ListUsersQuery>();

        CreateMap<ListUsersItemResult, ListUsersItemResponse>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => new ListUsersNameResponse
            {
                Firstname = src.Firstname,
                Lastname = src.Lastname
            }))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new ListUsersAddressResponse
            {
                City = src.City,
                Street = src.Street,
                Number = src.Number,
                Zipcode = src.Zipcode,
                Geolocation = new ListUsersGeolocationResponse { Lat = src.Lat, Long = src.Long }
            }));
    }
}
