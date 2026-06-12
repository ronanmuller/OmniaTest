using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using StackExchange.Redis;

namespace Ambev.DeveloperEvaluation.ORM.Cache;

public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IDatabase _cache;
    private readonly ResiliencePipeline _pipeline;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public CachedProductRepository(
        IProductRepository inner,
        IConnectionMultiplexer redis,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _inner    = inner;
        _cache    = redis.GetDatabase();
        _pipeline = pipelineProvider.GetPipeline("product-cache");
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = $"product:{id}";

        try
        {
            RedisValue cached = default;
            await _pipeline.ExecuteAsync(async ct =>
            {
                cached = await _cache.StringGetAsync(key);
            }, cancellationToken);

            if (cached.HasValue)
                return JsonSerializer.Deserialize<Product>(cached!);
        }
        catch (BrokenCircuitException) { /* circuit open — skip cache, go straight to DB */ }
        catch (Exception) { /* Redis unavailable — degrade gracefully */ }

        var product = await _inner.GetByIdAsync(id, cancellationToken);

        // best-effort write-back — fire and forget, never blocks the response
        if (product != null)
            _ = TrySetCacheAsync(key, product);

        return product;
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int size, string? order = null, string? category = null,
        CancellationToken cancellationToken = default)
        => _inner.GetPagedAsync(page, size, order, category, cancellationToken);

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var created = await _inner.CreateAsync(product, cancellationToken);
        _ = TryInvalidateAsync("product:categories");
        return created;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var updated = await _inner.UpdateAsync(product, cancellationToken);
        _ = TryInvalidateAsync($"product:{product.Id}", "product:categories");
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeleteAsync(id, cancellationToken);
        _ = TryInvalidateAsync($"product:{id}", "product:categories");
        return result;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string key = "product:categories";

        try
        {
            RedisValue cached = default;
            await _pipeline.ExecuteAsync(async ct =>
            {
                cached = await _cache.StringGetAsync(key);
            }, cancellationToken);

            if (cached.HasValue)
                return JsonSerializer.Deserialize<IEnumerable<string>>(cached!)!;
        }
        catch (BrokenCircuitException) { }
        catch (Exception) { }

        var categories = await _inner.GetCategoriesAsync(cancellationToken);
        _ = TrySetCacheAsync(key, categories);
        return categories;
    }

    private async Task TrySetCacheAsync<T>(string key, T value)
    {
        try
        {
            await _pipeline.ExecuteAsync(async _ =>
                await _cache.StringSetAsync(key, JsonSerializer.Serialize(value), Ttl));
        }
        catch { /* best-effort — cache miss is acceptable */ }
    }

    private async Task TryInvalidateAsync(params string[] keys)
    {
        try
        {
            await _pipeline.ExecuteAsync(async _ =>
            {
                foreach (var key in keys)
                    await _cache.KeyDeleteAsync(key);
            });
        }
        catch { /* best-effort — stale cache is preferable to a broken write path */ }
    }
}
