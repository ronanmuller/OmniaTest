using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Users.DeleteUser;

/// <summary>
/// Handler for processing DeleteUserCommand requests
/// </summary>
public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var success = await _userRepository.DeleteAsync(request.Id, cancellationToken);
        if (!success)
            throw new KeyNotFoundException($"User with ID {request.Id} not found");

        return new DeleteUserResponse { Success = true };
    }
}
