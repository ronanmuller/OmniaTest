using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Polly;
using Polly.Registry;
using StackExchange.Redis;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Cache;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CachedProductRepositoryTests
{
    private readonly IProductRepository _inner = Substitute.For<IProductRepository>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly CachedProductRepository _sut;

    public CachedProductRepositoryTests()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);

        // ResiliencePipeline.Empty is a passthrough — executes the callback directly,
        // no circuit breaker logic, so tests exercise the cache behavior in isolation.
        var provider = Substitute.For<ResiliencePipelineProvider<string>>();
        provider.GetPipeline("product-cache").Returns(ResiliencePipeline.Empty);

        _sut = new CachedProductRepository(_inner, redis, provider);
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync cache-miss: fetches from DB")]
    public async Task GetById_CacheMiss_FetchesFromDb()
    {
        var product = new Product { Id = Guid.NewGuid(), Title = "Widget", Price = 9.99m };
        var key = $"product:{product.Id}";

        _db.StringGetAsync(key, Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        _inner.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        await _inner.Received(1).GetByIdAsync(product.Id, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetByIdAsync cache-hit: returns cached product without calling DB")]
    public async Task GetById_CacheHit_ReturnsCachedProductWithoutDb()
    {
        var product = new Product { Id = Guid.NewGuid(), Title = "Widget", Price = 9.99m };
        var key = $"product:{product.Id}";
        var json = JsonSerializer.Serialize(product);

        _db.StringGetAsync(key, Arg.Any<CommandFlags>()).Returns(new RedisValue(json));

        var result = await _sut.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        await _inner.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetByIdAsync cache-miss with null DB result: does not populate cache")]
    public async Task GetById_CacheMiss_NullFromDb_DoesNotPopulateCache()
    {
        var id = Guid.NewGuid();
        _db.StringGetAsync($"product:{id}", Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        _inner.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
        await _db.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>());
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateAsync delegates to inner and invalidates cache")]
    public async Task UpdateAsync_DelegatesAndInvalidatesCache()
    {
        var product = new Product { Id = Guid.NewGuid(), Title = "Widget", Price = 9.99m };
        _inner.UpdateAsync(product, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.UpdateAsync(product);

        result.Should().Be(product);
        await _inner.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "DeleteAsync delegates to inner repository")]
    public async Task DeleteAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        await _inner.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync delegates to inner repository")]
    public async Task CreateAsync_DelegatesToInner()
    {
        var product = new Product { Id = Guid.NewGuid(), Title = "Widget", Price = 9.99m };
        _inner.CreateAsync(product, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.CreateAsync(product);

        result.Should().Be(product);
        await _inner.Received(1).CreateAsync(product, Arg.Any<CancellationToken>());
    }

    // ── GetCategoriesAsync ────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetCategoriesAsync cache-miss: fetches from DB")]
    public async Task GetCategories_CacheMiss_FetchesFromDb()
    {
        var categories = new[] { "Electronics", "Books" };
        const string key = "product:categories";

        _db.StringGetAsync(key, Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        _inner.GetCategoriesAsync(Arg.Any<CancellationToken>()).Returns(categories);

        var result = await _sut.GetCategoriesAsync();

        result.Should().BeEquivalentTo(categories);
        await _inner.Received(1).GetCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetCategoriesAsync cache-hit: returns cached result without DB call")]
    public async Task GetCategories_CacheHit_ReturnsCachedWithoutDb()
    {
        var categories = new[] { "Electronics", "Books" };
        const string key = "product:categories";
        var json = JsonSerializer.Serialize(categories);

        _db.StringGetAsync(key, Arg.Any<CommandFlags>()).Returns(new RedisValue(json));

        var result = await _sut.GetCategoriesAsync();

        result.Should().BeEquivalentTo(categories);
        await _inner.DidNotReceive().GetCategoriesAsync(Arg.Any<CancellationToken>());
    }
}
