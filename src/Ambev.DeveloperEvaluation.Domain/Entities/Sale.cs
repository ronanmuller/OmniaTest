using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a sale (cart) in the system. Acts as the aggregate root.
/// Customer and Branch are external identity references (denormalized).
/// </summary>
public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    // External Identity — denormalized customer reference
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // External Identity — denormalized branch reference
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<SaleItem> Items { get; set; } = [];

    public Sale()
    {
        CreatedAt = DateTime.UtcNow;
        SaleNumber = Guid.NewGuid().ToString("N")[..8].ToUpper();
    }

    /// <summary>
    /// Recalculates all item totals and the sale total.
    /// Must be called after adding or changing items.
    /// </summary>
    public void CalculateTotals()
    {
        var total = 0m;
        foreach (var item in Items.Where(i => !i.IsCancelled))
        {
            item.Calculate();
            total += item.TotalAmount;
        }
        TotalAmount = total;
    }

    public void Cancel()
    {
        IsCancelled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CancelItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in sale {Id}");

        item.IsCancelled = true;
        TotalAmount = Items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
        UpdatedAt = DateTime.UtcNow;
    }
}
