using Ambev.DeveloperEvaluation.Application.Common;

namespace Ambev.DeveloperEvaluation.Application.Users.EventHandlers.V1;

public record UserRegisteredEvent(
    Guid EventId,
    Guid UserId,
    string Username,
    string Email,
    string Role,
    Guid CorrelationId,
    Guid CausationId) : IDomainEvent
{
    public string SchemaVersion { get; init; } = "v1";
    public Guid AggregateId => UserId;
}
