using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Users.GetUser;

public class GetUserHandler : IRequestHandler<GetUserQuery, GetUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<GetUserResult> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {request.Id} not found");

        return _mapper.Map<GetUserResult>(user);
    }
}
