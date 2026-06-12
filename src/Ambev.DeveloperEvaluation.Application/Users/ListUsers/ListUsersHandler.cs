using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Users.ListUsers;

public class ListUsersHandler : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ListUsersHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ListUsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _userRepository.GetPagedAsync(
            request.Page, request.Size, request.Order, cancellationToken);

        return new ListUsersResult
        {
            Data = _mapper.Map<List<ListUsersItemResult>>(items),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = (int)Math.Ceiling(total / (double)request.Size)
        };
    }
}
