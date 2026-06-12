namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsResult
{
    public List<ListCartsItemResult> Data { get; set; } = [];
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class ListCartsItemResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<ListCartsItemProductResult> Products { get; set; } = [];
}

public class ListCartsItemProductResult
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
