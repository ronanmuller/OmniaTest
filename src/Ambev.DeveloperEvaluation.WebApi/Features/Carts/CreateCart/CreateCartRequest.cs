namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts.CreateCart;

public class CreateCartRequest
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<CreateCartItemRequest> Products { get; set; } = [];
}

public class CreateCartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
