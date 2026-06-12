using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public record ListSalesQuery : IRequest<ListSalesResult>
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
    public string? Order { get; init; } = "date desc";
    public Guid? CustomerId { get; init; }
    public Guid? BranchId { get; init; }
    public DateTime? MinDate { get; init; }
    public DateTime? MaxDate { get; init; }
    public bool? IsCancelled { get; init; }
}
