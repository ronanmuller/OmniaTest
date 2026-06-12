using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Application.Users.EventHandlers;
using Ambev.DeveloperEvaluation.Application.Users.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Application.Common;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class UserRegisteredEventHandlerTests
{
    private static readonly Guid CorrelationId = Guid.NewGuid();
    private static readonly Guid CausationId   = CorrelationId;

    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly IIdempotencyGuard _idempotency = Substitute.For<IIdempotencyGuard>();

    [Fact(DisplayName = "Given UserRegisteredEvent When handled Then persists to event store")]
    public async Task Handle_ValidEvent_PersistsToEventStore()
    {
        var handler = new UserRegisteredEventHandler(
            Substitute.For<ILogger<UserRegisteredEventHandler>>(),
            _eventStore,
            _idempotency);

        var evt = new UserRegisteredEvent(
            Guid.NewGuid(), Guid.NewGuid(), "johndoe", "john@example.com", "Customer",
            CorrelationId, CausationId);

        await handler.Handle(evt);

        await _eventStore.Received(1).SaveAsync(
            nameof(UserRegisteredEvent), evt.EventId, evt.UserId, evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given UserRegisteredEvent When handled Then completes without throwing")]
    public async Task Handle_ValidEvent_CompletesSuccessfully()
    {
        var handler = new UserRegisteredEventHandler(
            Substitute.For<ILogger<UserRegisteredEventHandler>>(),
            _eventStore,
            _idempotency);

        var evt = new UserRegisteredEvent(
            Guid.NewGuid(), Guid.NewGuid(), "jane", "jane@example.com", "Admin",
            CorrelationId, CausationId);

        var act = async () => await handler.Handle(evt);

        await act.Should().NotThrowAsync();
    }
}
