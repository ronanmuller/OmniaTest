using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleCommand : IRequest<CreateSaleResult>
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<CreateSaleItemCommand> Items { get; set; } = [];
}

public class CreateSaleItemCommand
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
