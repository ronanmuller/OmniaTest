namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Represents a product rating composed of a score and a review count.
/// Immutable value object — equality is by value, not identity.
/// </summary>
public record Rating(decimal Rate, int Count)
{
    public static readonly Rating Empty = new(0m, 0);
}
