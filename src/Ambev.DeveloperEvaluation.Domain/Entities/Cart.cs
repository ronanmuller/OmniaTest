using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }

    public List<CartItem> Products { get; set; } = [];
}

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }

    // External Identity — denormalized product reference (no FK to Products table)
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
