namespace Ambev.DeveloperEvaluation.Application.Common;

/// <summary>
/// Named keys for resilience pipelines registered in the IoC container.
/// Centralizing keys here avoids magic strings scattered across handlers.
/// </summary>
public static class ResilienceKeys
{
    public const string SaleReadModel  = "sale-read-model";
    public const string ProductCache   = "product-cache";
}
