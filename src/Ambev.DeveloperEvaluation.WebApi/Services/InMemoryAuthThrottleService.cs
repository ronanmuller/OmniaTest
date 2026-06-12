using Microsoft.Extensions.Caching.Memory;

namespace Ambev.DeveloperEvaluation.WebApi.Services;

/// <summary>
/// Lightweight lockout guard for the authentication endpoint.
/// Keeps the API safe from repeated credential attempts without exposing whether an email exists.
///
/// For a multi-instance production deployment this state should be moved to a distributed store
/// such as Redis, using the same key strategy.
/// </summary>
public sealed class InMemoryAuthThrottleService : IAuthThrottleService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public InMemoryAuthThrottleService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsLockedOut(string email, out DateTimeOffset lockedUntil)
    {
        var key = LockoutKey(email);
        if (_cache.TryGetValue<DateTimeOffset>(key, out lockedUntil))
            return lockedUntil > DateTimeOffset.UtcNow;

        lockedUntil = default;
        return false;
    }

    public void RecordFailure(string email)
    {
        var attemptsKey = AttemptsKey(email);
        var attempts = _cache.Get<int>(attemptsKey) + 1;

        _cache.Set(attemptsKey, attempts, FailureWindow);

        if (attempts >= MaxFailedAttempts)
        {
            var lockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
            _cache.Set(LockoutKey(email), lockedUntil, LockoutDuration);
            _cache.Remove(attemptsKey);
        }
    }

    public void RecordSuccess(string email)
    {
        _cache.Remove(AttemptsKey(email));
        _cache.Remove(LockoutKey(email));
    }

    private static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    private static string AttemptsKey(string email) => $"auth:failures:{Normalize(email)}";
    private static string LockoutKey(string email) => $"auth:lockout:{Normalize(email)}";
}
