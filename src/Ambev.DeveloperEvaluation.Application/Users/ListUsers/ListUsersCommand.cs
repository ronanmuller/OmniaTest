using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

public record ListUsersQuery : IRequest<ListUsersResult>
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
    public string? Order { get; init; }
}
