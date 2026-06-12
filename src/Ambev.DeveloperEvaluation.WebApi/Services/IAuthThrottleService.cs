namespace Ambev.DeveloperEvaluation.WebApi.Services;

public interface IAuthThrottleService
{
    bool IsLockedOut(string email, out DateTimeOffset lockedUntil);
    void RecordFailure(string email);
    void RecordSuccess(string email);
}
