namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;

public class UpdateProductResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public UpdateProductRatingResponse Rating { get; set; } = new();
}

public class UpdateProductRatingResponse
{
    public decimal Rate { get; set; }
    public int Count { get; set; }
}
