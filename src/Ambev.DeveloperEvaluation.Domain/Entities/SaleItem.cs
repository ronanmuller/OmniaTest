using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents an item within a sale.
/// Holds an external identity reference to the product (denormalized).
/// Business rules for discounts are enforced here.
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }

    // External Identity — denormalized product reference
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Applies quantity-based discount rules and calculates the item total.
    /// </summary>
    /// <exception cref="DomainException">Thrown when quantity exceeds 20.</exception>
    public void Calculate()
    {
        if (Quantity > 20)
            throw new DomainException($"Cannot sell more than 20 identical items. Product: {ProductTitle}");

        Discount = Quantity switch
        {
            >= 10 => 0.20m,
            >= 4 => 0.10m,
            _ => 0m
        };

        TotalAmount = Quantity * UnitPrice * (1 - Discount);
    }
}
