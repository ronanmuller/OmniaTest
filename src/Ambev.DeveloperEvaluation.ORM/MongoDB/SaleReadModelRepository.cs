using Ambev.DeveloperEvaluation.Domain.ReadModels;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.MongoDB;

public class SaleReadModelRepository : ISaleReadModelRepository
{
    private readonly IMongoCollection<SaleReadModel> _collection;

    public SaleReadModelRepository(IMongoClient client)
    {
        var db = client.GetDatabase("developer_evaluation_events");
        _collection = db.GetCollection<SaleReadModel>("sale_read_models");

        _collection.Indexes.CreateMany([
            new CreateIndexModel<SaleReadModel>(
                Builders<SaleReadModel>.IndexKeys.Ascending(s => s.SaleNumber),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<SaleReadModel>(
                Builders<SaleReadModel>.IndexKeys.Descending(s => s.Date)),
        ]);
    }

    public async Task<SaleReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _collection
            .Find(s => s.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<SaleReadModel> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null,
        Guid? customerId = null, Guid? branchId = null,
        DateTime? minDate = null, DateTime? maxDate = null, bool? isCancelled = null,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(customerId, branchId, minDate, maxDate, isCancelled);
        var sort = BuildSort(order);

        var total = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var items = await _collection
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task UpsertAsync(SaleReadModel model, CancellationToken cancellationToken = default)
    {
        model.ProjectedAt = DateTime.UtcNow;

        await _collection.ReplaceOneAsync(
            s => s.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task SetCancelledAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var update = Builders<SaleReadModel>.Update
            .Set(s => s.IsCancelled, true)
            .Set(s => s.ProjectedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(s => s.Id == saleId, update, cancellationToken: cancellationToken);
    }

    public async Task SetItemCancelledAsync(Guid saleId, Guid itemId, CancellationToken cancellationToken = default)
    {
        // MongoDB positional operator: encontra o item no array e atualiza só ele
        var filter = Builders<SaleReadModel>.Filter.And(
            Builders<SaleReadModel>.Filter.Eq(s => s.Id, saleId),
            Builders<SaleReadModel>.Filter.ElemMatch(s => s.Items, i => i.Id == itemId));

        var update = Builders<SaleReadModel>.Update
            .Set("Items.$.IsCancelled", true)
            .Set(s => s.ProjectedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static FilterDefinition<SaleReadModel> BuildFilter(
        Guid? customerId, Guid? branchId, DateTime? minDate, DateTime? maxDate, bool? isCancelled)
    {
        var filters = new List<FilterDefinition<SaleReadModel>>();

        if (customerId.HasValue)
            filters.Add(Builders<SaleReadModel>.Filter.Eq(s => s.CustomerId, customerId.Value));

        if (branchId.HasValue)
            filters.Add(Builders<SaleReadModel>.Filter.Eq(s => s.BranchId, branchId.Value));

        if (minDate.HasValue)
            filters.Add(Builders<SaleReadModel>.Filter.Gte(s => s.Date, minDate.Value));

        if (maxDate.HasValue)
            filters.Add(Builders<SaleReadModel>.Filter.Lte(s => s.Date, maxDate.Value));

        if (isCancelled.HasValue)
            filters.Add(Builders<SaleReadModel>.Filter.Eq(s => s.IsCancelled, isCancelled.Value));

        return filters.Count == 0
            ? Builders<SaleReadModel>.Filter.Empty
            : Builders<SaleReadModel>.Filter.And(filters);
    }

    private static SortDefinition<SaleReadModel> BuildSort(string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return Builders<SaleReadModel>.Sort.Descending(s => s.Date);

        SortDefinition<SaleReadModel>? sort = null;

        foreach (var part in order.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tokens = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = tokens[0].ToLower();
            var desc = tokens.Length > 1 && tokens[1].ToLower() == "desc";

            var next = (field, desc) switch
            {
                ("date", false)         => Builders<SaleReadModel>.Sort.Ascending(s => s.Date),
                ("date", true)          => Builders<SaleReadModel>.Sort.Descending(s => s.Date),
                ("totalamount", false)  => Builders<SaleReadModel>.Sort.Ascending(s => s.TotalAmount),
                ("totalamount", true)   => Builders<SaleReadModel>.Sort.Descending(s => s.TotalAmount),
                ("customername", false) => Builders<SaleReadModel>.Sort.Ascending(s => s.CustomerName),
                ("customername", true)  => Builders<SaleReadModel>.Sort.Descending(s => s.CustomerName),
                ("salenumber", false)   => Builders<SaleReadModel>.Sort.Ascending(s => s.SaleNumber),
                ("salenumber", true)    => Builders<SaleReadModel>.Sort.Descending(s => s.SaleNumber),
                _                       => Builders<SaleReadModel>.Sort.Descending(s => s.Date)
            };

            sort = sort == null ? next : Builders<SaleReadModel>.Sort.Combine(sort, next);
        }

        return sort ?? Builders<SaleReadModel>.Sort.Descending(s => s.Date);
    }
}
