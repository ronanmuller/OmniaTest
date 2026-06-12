namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;

public class GetSaleResponse
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public List<GetSaleItemResponse> Items { get; set; } = [];
}

public class GetSaleItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
}
