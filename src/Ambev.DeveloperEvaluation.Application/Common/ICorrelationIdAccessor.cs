namespace Ambev.DeveloperEvaluation.Application.Common;

/// <summary>
/// Provides the CorrelationId for the current operation scope (HTTP request, background job, etc.).
/// Implemented in the WebApi layer via IHttpContextAccessor so the Application layer
/// has no direct dependency on HttpContext.
/// </summary>
public interface ICorrelationIdAccessor
{
    Guid CorrelationId { get; }
}
