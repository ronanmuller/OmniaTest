using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Users.EventHandlers;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;
using Ambev.DeveloperEvaluation.Common.Security;

namespace Ambev.DeveloperEvaluation.Application.Users.CreateUser;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserRepository userRepository,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        IDomainEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationId,
        ILogger<CreateUserHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _eventPublisher = eventPublisher;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email {Email}", command.Email);

        var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

        var uniqueEmail = new UniqueEmailSpecification();
        if (!uniqueEmail.IsSatisfiedBy(existingUser))
        {
            _logger.LogWarning("User creation rejected: email {Email} already exists", command.Email);
            throw new InvalidOperationException($"User with email {command.Email} already exists");
        }

        var user = _mapper.Map<User>(command);
        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();
        user.Status = Domain.Enums.UserStatus.Active;
        user.Password = _passwordHasher.HashPassword(command.Password);

        var correlationId = _correlationId.CorrelationId;

        try
        {
            await _eventPublisher.PublishAsync(new UserRegisteredEvent(
                Guid.NewGuid(),
                user.Id,
                user.Username,
                user.Email,
                user.Role.ToString(),
                correlationId,
                CausationId: correlationId), cancellationToken);

            var created = await _userRepository.CreateAsync(user, cancellationToken);

            _logger.LogInformation("User {UserId} ({Username}) created with role {Role}",
                created.Id, created.Username, created.Role);

            return _mapper.Map<CreateUserResult>(created);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogWarning(ex, "Race condition: email {Email} inserted concurrently", command.Email);
            throw new InvalidOperationException($"User with email {command.Email} already exists", ex);
        }
    }
}
