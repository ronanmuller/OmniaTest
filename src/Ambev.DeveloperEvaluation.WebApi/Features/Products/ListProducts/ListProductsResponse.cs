namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;

public class ListProductsItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public ListProductsRatingResponse Rating { get; set; } = new();
}

public class ListProductsRatingResponse
{
    public decimal Rate { get; set; }
    public int Count { get; set; }
}
