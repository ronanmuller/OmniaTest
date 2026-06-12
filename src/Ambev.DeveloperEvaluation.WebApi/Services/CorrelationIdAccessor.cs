using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.WebApi.Middleware;

namespace Ambev.DeveloperEvaluation.WebApi.Services;

/// <summary>
/// Reads the CorrelationId injected by CorrelationIdMiddleware from HttpContext.Items.
/// Falls back to a new Guid when called outside an HTTP request (background workers, tests).
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid CorrelationId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName] as string;
            return Guid.TryParse(raw, out var id) ? id : Guid.NewGuid();
        }
    }
}
