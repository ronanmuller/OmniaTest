namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Represents a physical address. Immutable value object — equality is by value, not identity.
/// </summary>
public record Address(
    string City,
    string Street,
    int Number,
    string Zipcode,
    string Lat,
    string Long)
{
    public static readonly Address Empty = new(string.Empty, string.Empty, 0, string.Empty, string.Empty, string.Empty);
}
