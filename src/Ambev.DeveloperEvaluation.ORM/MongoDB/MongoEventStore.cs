using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.MongoDB;

public class MongoEventStore : IEventStore
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoEventStore(IMongoClient client)
    {
        var db = client.GetDatabase("developer_evaluation_events");
        _collection = db.GetCollection<BsonDocument>("sale_events");

        var compoundIndex = Builders<BsonDocument>.IndexKeys
            .Ascending("aggregateId")
            .Ascending("occurredAt");

        var uniqueEventId = Builders<BsonDocument>.IndexKeys.Ascending("eventId");

        _collection.Indexes.CreateMany([
            new CreateIndexModel<BsonDocument>(compoundIndex),
            new CreateIndexModel<BsonDocument>(uniqueEventId, new CreateIndexOptions { Unique = true })
        ]);
    }

    public async Task SaveAsync(string eventType, Guid eventId, Guid aggregateId, object payload, CancellationToken cancellationToken = default)
    {
        var doc = new BsonDocument
        {
            ["eventId"]     = eventId.ToString(),
            ["eventType"]   = eventType,
            ["aggregateId"] = aggregateId.ToString(),
            ["occurredAt"]  = DateTime.UtcNow,
            ["payload"]     = BsonDocument.Parse(JsonSerializer.Serialize(payload))
        };

        try
        {
            await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Idempotência: evento já processado anteriormente — ignorar silenciosamente.
        }
    }
}
