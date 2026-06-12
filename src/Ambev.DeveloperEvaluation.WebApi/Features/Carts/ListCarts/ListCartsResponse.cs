namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.ListCarts;

public class ListCartsResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<ListCartsItemResponse> Products { get; set; } = [];
}

public class ListCartsItemResponse
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
