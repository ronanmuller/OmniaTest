namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.UpdateCart;

public class UpdateCartRequest
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<UpdateCartItemRequest> Products { get; set; } = [];
}

public class UpdateCartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
