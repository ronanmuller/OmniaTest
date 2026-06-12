namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.GetCart;

public class GetCartResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<GetCartItemResponse> Products { get; set; } = [];
}

public class GetCartItemResponse
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
