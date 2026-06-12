using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Common.Security;

namespace Ambev.DeveloperEvaluation.Application.Users.UpdateUser;

/// <summary>
/// Handler for processing UpdateUserCommand requests
/// </summary>
public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UpdateUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserHandler(IUserRepository userRepository, IMapper mapper, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<UpdateUserResult> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {command.Id} not found");

        _mapper.Map(command, user);
        user.Password = _passwordHasher.HashPassword(command.Password);
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(user, cancellationToken);
        return _mapper.Map<UpdateUserResult>(updated);
    }
}
