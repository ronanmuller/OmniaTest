namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.UpdateCart;

public class UpdateCartResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<UpdateCartItemResponse> Products { get; set; } = [];
}

public class UpdateCartItemResponse
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
