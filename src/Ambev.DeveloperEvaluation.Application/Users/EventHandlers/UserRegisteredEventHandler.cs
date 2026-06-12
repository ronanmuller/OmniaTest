using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Users.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Users.EventHandlers;

public class UserRegisteredEventHandler : DomainEventHandlerBase<UserRegisteredEvent>
{
    public UserRegisteredEventHandler(
        ILogger<UserRegisteredEventHandler> logger,
        IEventStore eventStore,
        IIdempotencyGuard idempotency)
        : base(logger, eventStore, idempotency) { }

    protected override async Task ProcessAsync(UserRegisteredEvent message, IEventStore eventStore)
    {
        Logger.LogInformation(
            "UserRegistered detail: Username={Username} Email={Email} Role={Role}",
            message.Username, message.Email, message.Role);

        await eventStore.SaveAsync(message);
    }
}
