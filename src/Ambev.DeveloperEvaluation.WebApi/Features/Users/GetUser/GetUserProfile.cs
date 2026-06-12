using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;

public class GetUserProfile : Profile
{
    public GetUserProfile()
    {
        CreateMap<Guid, Application.Users.GetUser.GetUserQuery>()
            .ConstructUsing(id => new Application.Users.GetUser.GetUserQuery(id));
        CreateMap<Application.Users.GetUser.GetUserResult, GetUserResponse>();
    }
}
