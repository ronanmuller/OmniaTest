using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Ambev.DeveloperEvaluation.Domain.ReadModels;

/// <summary>
/// Denormalized read model projected from Sale domain events.
/// Stored in MongoDB — optimized for queries, not writes.
/// May lag behind PostgreSQL by a few seconds (eventual consistency).
/// </summary>
public class SaleReadModel
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public List<SaleItemReadModel> Items { get; set; } = [];
    public DateTime ProjectedAt { get; set; }
}

public class SaleItemReadModel
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
}
