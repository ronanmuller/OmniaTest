using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Application.Carts.GetCart;
using Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;
using Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CartHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    // ─── CreateCart ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Given valid command When creating cart Then returns result with cart data")]
    public async Task CreateCart_ValidCommand_ReturnsCartResult()
    {
        // Given
        var command = CreateCartHandlerTestData.GenerateValidCommand();
        var cart = new Cart { Id = Guid.NewGuid(), UserId = command.UserId, Date = command.Date };
        var expectedResult = new CreateCartResult { Id = cart.Id, UserId = cart.UserId };

        _mapper.Map<Cart>(command).Returns(cart);
        _cartRepository.CreateAsync(cart, Arg.Any<CancellationToken>()).Returns(cart);
        _mapper.Map<CreateCartResult>(cart).Returns(expectedResult);

        var handler = new CreateCartHandler(_cartRepository, _mapper);

        // When
        var result = await handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(cart.Id);
        await _cartRepository.Received(1).CreateAsync(cart, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given valid command When creating cart Then calls repository once")]
    public async Task CreateCart_ValidCommand_CallsRepositoryOnce()
    {
        // Given
        var command = CreateCartHandlerTestData.GenerateValidCommand();
        var cart = new Cart { Id = Guid.NewGuid() };

        _mapper.Map<Cart>(command).Returns(cart);
        _cartRepository.CreateAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>()).Returns(cart);
        _mapper.Map<CreateCartResult>(Arg.Any<Cart>()).Returns(new CreateCartResult());

        var handler = new CreateCartHandler(_cartRepository, _mapper);

        // When
        await handler.Handle(command, CancellationToken.None);

        // Then
        await _cartRepository.Received(1).CreateAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>());
    }

    // ─── GetCart ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Given existing cart id When getting cart Then returns cart data")]
    public async Task GetCart_ExistingId_ReturnsCart()
    {
        // Given
        var cartId = Guid.NewGuid();
        var cart = new Cart { Id = cartId, UserId = Guid.NewGuid(), Date = DateTime.UtcNow };
        var expectedResult = new GetCartResult { Id = cartId };

        _cartRepository.GetByIdAsync(cartId, Arg.Any<CancellationToken>()).Returns(cart);
        _mapper.Map<GetCartResult>(cart).Returns(expectedResult);

        var handler = new GetCartHandler(_cartRepository, _mapper);

        // When
        var result = await handler.Handle(new GetCartQuery(cartId), CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(cartId);
    }

    [Fact(DisplayName = "Given non-existing cart id When getting cart Then throws KeyNotFoundException")]
    public async Task GetCart_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Given
        var cartId = Guid.NewGuid();
        _cartRepository.GetByIdAsync(cartId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new GetCartHandler(_cartRepository, _mapper);

        // When
        var act = async () => await handler.Handle(new GetCartQuery(cartId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{cartId}*");
    }

    // ─── UpdateCart ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Given existing cart When updating Then returns updated result")]
    public async Task UpdateCart_ExistingCart_ReturnsUpdatedResult()
    {
        // Given
        var cartId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cart = new Cart { Id = cartId, UserId = userId };
        var command = new UpdateCartCommand
        {
            Id = cartId,
            UserId = userId,
            Date = DateTime.UtcNow,
            Products = [new UpdateCartItemCommand { ProductId = Guid.NewGuid(), Quantity = 2 }]
        };
        var expectedResult = new UpdateCartResult { Id = cartId, UserId = userId };

        _cartRepository.GetByIdAsync(cartId, Arg.Any<CancellationToken>()).Returns(cart);
        _cartRepository.UpdateAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>()).Returns(cart);
        _mapper.Map<UpdateCartResult>(Arg.Any<Cart>()).Returns(expectedResult);

        var handler = new UpdateCartHandler(_cartRepository, _mapper);

        // When
        var result = await handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(cartId);
        await _cartRepository.Received(1).UpdateAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existing cart When updating Then throws KeyNotFoundException")]
    public async Task UpdateCart_NonExistingCart_ThrowsKeyNotFoundException()
    {
        // Given
        var cartId = Guid.NewGuid();
        _cartRepository.GetByIdAsync(cartId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var command = new UpdateCartCommand { Id = cartId, UserId = Guid.NewGuid(), Date = DateTime.UtcNow };
        var handler = new UpdateCartHandler(_cartRepository, _mapper);

        // When
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{cartId}*");
    }

    // ─── DeleteCart ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Given existing cart When deleting Then completes successfully")]
    public async Task DeleteCart_ExistingCart_CompletesSuccessfully()
    {
        // Given
        var cartId = Guid.NewGuid();
        _cartRepository.DeleteAsync(cartId, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteCartHandler(_cartRepository);

        // When
        var act = async () => await handler.Handle(new DeleteCartCommand(cartId), CancellationToken.None);

        // Then
        await act.Should().NotThrowAsync();
        await _cartRepository.Received(1).DeleteAsync(cartId, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existing cart When deleting Then throws KeyNotFoundException")]
    public async Task DeleteCart_NonExistingCart_ThrowsKeyNotFoundException()
    {
        // Given
        var cartId = Guid.NewGuid();
        _cartRepository.DeleteAsync(cartId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteCartHandler(_cartRepository);

        // When
        var act = async () => await handler.Handle(new DeleteCartCommand(cartId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{cartId}*");
    }
}
