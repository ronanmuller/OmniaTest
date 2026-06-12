using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

public class CreateCartCommand : IRequest<CreateCartResult>
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public List<CreateCartItemCommand> Products { get; set; } = [];
}

public class CreateCartItemCommand
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
