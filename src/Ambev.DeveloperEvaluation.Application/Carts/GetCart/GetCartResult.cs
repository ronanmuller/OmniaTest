namespace Ambev.DeveloperEvaluation.Application.Carts.GetCart;

public class GetCartResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<GetCartItemResult> Products { get; set; } = [];
}

public class GetCartItemResult
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
