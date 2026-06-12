using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;

namespace Ambev.DeveloperEvaluation.Integration.Users;

public class UserRepositoryTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DefaultContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync: persists user and returns with same Id")]
    public async Task CreateAsync_ValidUser_PersistsAndReturnsUser()
    {
        var user = BuildUser("joao@teste.com");

        var created = await _repository.CreateAsync(user);

        var fromDb = await _context.Users.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Email.Should().Be("joao@teste.com");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync: returns correct user")]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        var user = BuildUser("maria@teste.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("maria@teste.com");
    }

    [Fact(DisplayName = "GetByIdAsync: returns null for unknown id")]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── GetByEmailAsync ───────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByEmailAsync: returns user by email")]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var user = BuildUser("pedro@teste.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByEmailAsync("pedro@teste.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact(DisplayName = "GetByEmailAsync: returns null for unknown email")]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var result = await _repository.GetByEmailAsync("naoexiste@teste.com");
        result.Should().BeNull();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetPagedAsync: returns correct total count")]
    public async Task GetPagedAsync_MultipleUsers_ReturnsTotalCount()
    {
        await _context.Users.AddRangeAsync(
            BuildUser("a@teste.com"),
            BuildUser("b@teste.com"),
            BuildUser("c@teste.com"));
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(1, 10);

        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "GetPagedAsync: pagination works correctly")]
    public async Task GetPagedAsync_SecondPage_ReturnsCorrectItems()
    {
        await _context.Users.AddRangeAsync(Enumerable.Range(1, 5).Select(i => BuildUser($"u{i}@teste.com")));
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetPagedAsync(2, 2);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetPagedAsync: ordering by email desc works")]
    public async Task GetPagedAsync_OrderByEmailDesc_ReturnsSortedItems()
    {
        await _context.Users.AddRangeAsync(
            BuildUser("charlie@teste.com"),
            BuildUser("alice@teste.com"),
            BuildUser("bob@teste.com"));
        await _context.SaveChangesAsync();

        var (items, _) = await _repository.GetPagedAsync(1, 10, "email desc");

        items[0].Email.Should().Be("charlie@teste.com");
        items[1].Email.Should().Be("bob@teste.com");
        items[2].Email.Should().Be("alice@teste.com");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateAsync: persists changes")]
    public async Task UpdateAsync_ExistingUser_PersistsChanges()
    {
        var user = BuildUser("update@teste.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toUpdate = await _context.Users.FindAsync(user.Id);
        toUpdate!.Username = "novousername";
        await _repository.UpdateAsync(toUpdate);

        _context.ChangeTracker.Clear();
        var fromDb = await _context.Users.FindAsync(user.Id);
        fromDb!.Username.Should().Be("novousername");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "DeleteAsync: removes user and returns true")]
    public async Task DeleteAsync_ExistingUser_RemovesAndReturnsTrue()
    {
        var user = BuildUser("delete@teste.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(user.Id);

        result.Should().BeTrue();
        var fromDb = await _context.Users.FindAsync(user.Id);
        fromDb.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteAsync: returns false for unknown id")]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static User BuildUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        Username = email.Split('@')[0],
        Email = email,
        Phone = "+5511999999999",
        Password = "hashed_password",
        Role = UserRole.Customer,
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
    };
}
