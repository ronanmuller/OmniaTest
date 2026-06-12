using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleCommand : IRequest<UpdateSaleResult>
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<UpdateSaleItemCommand> Items { get; set; } = [];
}

public class UpdateSaleItemCommand
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
