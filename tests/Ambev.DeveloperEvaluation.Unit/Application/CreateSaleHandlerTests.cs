using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _eventPublisher = Substitute.For<IDomainEventPublisher>();
        var correlationId = Substitute.For<ICorrelationIdAccessor>();
        correlationId.CorrelationId.Returns(Guid.NewGuid());
        _handler = new CreateSaleHandler(_saleRepository, _mapper, _eventPublisher, correlationId,
            Substitute.For<ILogger<CreateSaleHandler>>());
    }

    [Fact(DisplayName = "Given valid command When creating sale Then persists to repository")]
    public async Task Handle_ValidCommand_PersistsSale()
    {
        // Given
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        var sale = BuildSaleFromCommand(command);
        SetupMapper(command, sale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(sale.Id);
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given command with items When creating sale Then published event contains all items")]
    public async Task Handle_CommandWithItems_PublishedEventContainsItems()
    {
        // Given
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        // Ensure we have a known number of items
        command.Items = [
            new() { ProductId = Guid.NewGuid(), ProductTitle = "Cerveja", UnitPrice = 10m, Quantity = 5 },
            new() { ProductId = Guid.NewGuid(), ProductTitle = "Refrigerante", UnitPrice = 5m, Quantity = 2 }
        ];

        var sale = BuildSaleFromCommand(command);
        SetupMapper(command, sale);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then — event must carry both items so the event handler can project them to MongoDB
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<SaleCreatedEvent>(e =>
                e.Items.Count == 2 &&
                e.Items.All(i => i.ItemId != Guid.Empty) &&
                e.Items.All(i => i.Quantity > 0) &&
                e.Items.All(i => i.UnitPrice > 0)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given command with items When creating sale Then event item IDs match sale item IDs")]
    public async Task Handle_CommandWithItems_EventItemIdsMatchSaleItemIds()
    {
        // Given
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        var sale = BuildSaleFromCommand(command);
        SetupMapper(command, sale);

        SaleCreatedEvent? captured = null;
        await _eventPublisher.PublishAsync(
            Arg.Do<SaleCreatedEvent>(e => captured = e),
            Arg.Any<CancellationToken>());

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then — item IDs in the event must match the actual sale items (not new Guids)
        captured.Should().NotBeNull();
        captured!.Items.Select(i => i.ItemId)
            .Should().BeEquivalentTo(sale.Items.Select(i => i.Id),
                "event item IDs must reference the real SaleItem IDs so the read model is consistent");
    }

    [Fact(DisplayName = "Given command When creating sale Then event is published before repository save")]
    public async Task Handle_ValidCommand_EventPublishedBeforeRepositorySave()
    {
        // Given
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        var sale = BuildSaleFromCommand(command);
        SetupMapper(command, sale);

        var callOrder = new List<string>();
        _eventPublisher.PublishAsync(Arg.Any<SaleCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("event"); return Task.CompletedTask; });
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("repo"); return Task.FromResult(sale); });

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then — event before repo: if repo fails, event is not yet in the outbox (atomic with repo save)
        // Actually in the current design: event is published first, repo second — this is intentional
        // (OutboxEventPublisher writes to the outbox table which is saved atomically WITH the repo in a transaction)
        callOrder.Should().Equal("event", "repo");
    }

    [Fact(DisplayName = "Given item quantity over 20 When creating sale Then throws DomainException")]
    public async Task Handle_ItemQuantityOver20_ThrowsDomainException()
    {
        // Given
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        command.Items = [new() { ProductId = Guid.NewGuid(), ProductTitle = "Produto", UnitPrice = 10m, Quantity = 21 }];

        _mapper.Map<SaleItem>(Arg.Any<CreateSaleItemCommand>())
            .Returns(c => new SaleItem
            {
                ProductId = ((CreateSaleItemCommand)c[0]).ProductId,
                ProductTitle = ((CreateSaleItemCommand)c[0]).ProductTitle,
                UnitPrice = ((CreateSaleItemCommand)c[0]).UnitPrice,
                Quantity = ((CreateSaleItemCommand)c[0]).Quantity,
            });

        // When / Then
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Ambev.DeveloperEvaluation.Domain.Exceptions.DomainException>()
            .WithMessage("*21*");
    }

    [Fact(DisplayName = "Given items with discount quantities When creating sale Then event total reflects discounts")]
    public async Task Handle_ItemsWithDiscount_EventTotalReflectsDiscounts()
    {
        // Given — 4 units triggers 10% discount
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        command.Items = [new() { ProductId = Guid.NewGuid(), ProductTitle = "Prod", UnitPrice = 100m, Quantity = 4 }];

        Sale? capturedSale = null;
        _mapper.Map<SaleItem>(Arg.Any<CreateSaleItemCommand>())
            .Returns(c =>
            {
                var cmd = (CreateSaleItemCommand)c[0];
                return new SaleItem { ProductId = cmd.ProductId, ProductTitle = cmd.ProductTitle, UnitPrice = cmd.UnitPrice, Quantity = cmd.Quantity };
            });
        _saleRepository.CreateAsync(Arg.Do<Sale>(s => capturedSale = s), Arg.Any<CancellationToken>())
            .Returns(c => (Sale)c[0]);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(new CreateSaleResult());

        SaleCreatedEvent? capturedEvent = null;
        await _eventPublisher.PublishAsync(Arg.Do<SaleCreatedEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>());

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then — 4 units * 100 * 0.9 = 360
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Items[0].Discount.Should().Be(0.10m, "4 units triggers 10% discount");
        capturedEvent.Items[0].TotalAmount.Should().Be(360m);
        capturedEvent.TotalAmount.Should().Be(360m);
    }

    private Sale BuildSaleFromCommand(CreateSaleCommand command)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = command.SaleNumber,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName,
            Items = command.Items.Select(i => new SaleItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                ProductTitle = i.ProductTitle,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
            }).ToList()
        };
        sale.CalculateTotals();
        return sale;
    }

    private void SetupMapper(CreateSaleCommand command, Sale sale)
    {
        _mapper.Map<SaleItem>(Arg.Any<CreateSaleItemCommand>())
            .Returns(c =>
            {
                var cmd = (CreateSaleItemCommand)c[0];
                return sale.Items.FirstOrDefault(i => i.ProductId == cmd.ProductId)
                    ?? new SaleItem { ProductId = cmd.ProductId, ProductTitle = cmd.ProductTitle, UnitPrice = cmd.UnitPrice, Quantity = cmd.Quantity };
            });
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<CreateSaleResult>(sale).Returns(new CreateSaleResult { Id = sale.Id, SaleNumber = sale.SaleNumber });
    }
}
