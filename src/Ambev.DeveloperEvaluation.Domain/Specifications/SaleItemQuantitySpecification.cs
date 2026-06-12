using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Specifications;

/// <summary>
/// Satisfied when the item quantity is within the allowed range (1–20).
/// Mirrors the discount rule boundary enforced in SaleItem.Calculate().
/// </summary>
public class SaleItemQuantitySpecification : ISpecification<SaleItem>
{
    private const int Min = 1;
    private const int Max = 20;

    public bool IsSatisfiedBy(SaleItem item) => item.Quantity is >= Min and <= Max;
}
