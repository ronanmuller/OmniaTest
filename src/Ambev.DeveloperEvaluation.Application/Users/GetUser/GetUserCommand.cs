using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

public record GetUserQuery(Guid Id) : IRequest<GetUserResult>;
