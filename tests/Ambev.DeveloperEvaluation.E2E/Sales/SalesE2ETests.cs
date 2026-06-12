using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.E2E.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.E2E.Sales;

/// <summary>
/// End-to-end tests for the Sale aggregate that exercise the full async flow:
///
///   HTTP → PostgreSQL (atomic) → OutboxProcessor → RabbitMQ → EventHandler → MongoDB
///
/// These tests REQUIRE docker-compose up with all containers healthy.
/// They are slower than functional tests (~10-15s each) because they wait for the
/// OutboxProcessor to poll (every 5s) and the event to be projected to MongoDB.
///
/// What these tests catch that unit/integration/functional tests cannot:
/// - SaleCreatedEvent missing items field → MongoDB receives Items = []
/// - OutboxProcessor deserializing events incorrectly (wrong type names, missing fields)
/// - SaleCreatedEventHandler projecting wrong fields to the read model
/// - RabbitMQ routing misconfiguration (event never arrives at handler)
/// - MongoDB document structure mismatch (GuidRepresentation, field names)
/// </summary>
[Trait("Category", "E2E")]
[Trait("Requires", "Docker")]
public class SalesE2ETests : E2ETestBase
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid BranchId   = Guid.NewGuid();
    private static readonly Guid ProductId  = Guid.NewGuid();

    public SalesE2ETests(DockerWebApplicationFactory factory) : base(factory) { }

    [Fact(DisplayName = "E2E: POST /api/Sales → MongoDB read model contains sale with items populated")]
    public async Task CreateSale_FullAsyncFlow_ProjectsToMongoWithItems()
    {
        // Given
        var token = await CreateUserAndGetTokenAsync("e2e-create@test.com");
        SetBearerToken(token);

        var saleNumber = $"E2E-{Guid.NewGuid().ToString("N")[..8]}";

        // When — create sale via HTTP (saves to PostgreSQL + outbox atomically)
        var createResp = await Client.PostAsJsonAsync("/api/Sales", new
        {
            saleNumber,
            customerId   = CustomerId,
            customerName = "Cliente E2E",
            branchId     = BranchId,
            branchName   = "Filial E2E",
            items = new[]
            {
                new
                {
                    productId    = ProductId,
                    productTitle = "Produto E2E",
                    unitPrice    = 50m,
                    quantity     = 5   // 5 items → 10% discount
                }
            }
        });

        createResp.StatusCode.Should().Be(HttpStatusCode.Created,
            because: await createResp.Content.ReadAsStringAsync());

        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = Guid.Parse(createDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!);

        // Then — wait for OutboxProcessor → RabbitMQ → SaleCreatedEventHandler → MongoDB
        // OutboxProcessor polls every 5s. WaitForReadModelAsync polls every 1s for up to 15s.
        var readModel = await WaitForReadModelAsync(
            saleId,
            m => m.Items.Count > 0,
            timeout: TimeSpan.FromSeconds(15));

        readModel.Should().NotBeNull(
            because: "SaleCreatedEventHandler must project the sale to MongoDB within 15 seconds");

        readModel!.SaleNumber.Should().Be(saleNumber);
        readModel.Items.Should().HaveCount(1,
            because: "SaleCreatedEvent must carry items — historically Items was always []");
        readModel.Items[0].ProductTitle.Should().Be("Produto E2E");
        readModel.Items[0].Quantity.Should().Be(5);
        readModel.Items[0].Discount.Should().Be(0.10m,
            because: "5 units must trigger 10% discount");
        readModel.Items[0].TotalAmount.Should().Be(225m,
            because: "5 * 50 * 0.9 = 225");
        readModel.IsCancelled.Should().BeFalse();
    }

    [Fact(DisplayName = "E2E: DELETE /api/Sales/{id} → MongoDB read model marks sale as cancelled")]
    public async Task CancelSale_FullAsyncFlow_UpdatesReadModelIsCancelled()
    {
        // Given — create a sale first
        var token = await CreateUserAndGetTokenAsync("e2e-cancel@test.com");
        SetBearerToken(token);

        var saleNumber = $"E2E-{Guid.NewGuid().ToString("N")[..8]}";

        var createResp = await Client.PostAsJsonAsync("/api/Sales", new
        {
            saleNumber,
            customerId   = CustomerId,
            customerName = "Cliente E2E",
            branchId     = BranchId,
            branchName   = "Filial E2E",
            items = new[]
            {
                new { productId = ProductId, productTitle = "Produto E2E", unitPrice = 10m, quantity = 1 }
            }
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = Guid.Parse(createDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!);

        // Wait for initial projection to MongoDB before cancelling
        var initial = await WaitForReadModelAsync(saleId, m => m.Items.Count > 0);
        initial.Should().NotBeNull(because: "sale must be projected before we cancel it");

        // When — cancel the sale
        var deleteResp = await Client.DeleteAsync($"/api/Sales/{saleId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Then — wait for SaleCancelledEventHandler to update MongoDB
        var cancelled = await WaitForReadModelAsync(
            saleId,
            m => m.IsCancelled,
            timeout: TimeSpan.FromSeconds(15));

        cancelled.Should().NotBeNull(
            because: "SaleCancelledEventHandler must update IsCancelled in MongoDB");
        cancelled!.IsCancelled.Should().BeTrue();
    }

    [Fact(DisplayName = "E2E: PATCH cancel item → MongoDB read model marks item as cancelled")]
    public async Task CancelSaleItem_FullAsyncFlow_UpdatesReadModelItemIsCancelled()
    {
        // Given — create a sale with one item
        var token = await CreateUserAndGetTokenAsync("e2e-cancel-item@test.com");
        SetBearerToken(token);

        var saleNumber = $"E2E-{Guid.NewGuid().ToString("N")[..8]}";

        var createResp = await Client.PostAsJsonAsync("/api/Sales", new
        {
            saleNumber,
            customerId   = CustomerId,
            customerName = "Cliente E2E",
            branchId     = BranchId,
            branchName   = "Filial E2E",
            items = new[]
            {
                new { productId = ProductId, productTitle = "Produto E2E", unitPrice = 20m, quantity = 4 }
            }
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = Guid.Parse(createDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!);
        var itemId = Guid.Parse(createDoc.RootElement
            .GetProperty("data").GetProperty("items")[0].GetProperty("id").GetString()!);

        // Wait for projection to MongoDB before cancelling the item
        var initial = await WaitForReadModelAsync(saleId, m => m.Items.Count > 0);
        initial.Should().NotBeNull(because: "sale must be projected before we cancel an item");

        // When — cancel one item
        var patchResp = await Client.PatchAsync($"/api/Sales/{saleId}/items/{itemId}/cancel", null);
        patchResp.StatusCode.Should().Be(HttpStatusCode.OK,
            because: await patchResp.Content.ReadAsStringAsync());

        // Then — wait for SaleItemCancelledEventHandler to update MongoDB
        var updated = await WaitForReadModelAsync(
            saleId,
            m => m.Items.Any(i => i.Id == itemId && i.IsCancelled),
            timeout: TimeSpan.FromSeconds(15));

        updated.Should().NotBeNull(
            because: "SaleItemCancelledEventHandler must mark the item as cancelled in MongoDB");
        updated!.Items.First(i => i.Id == itemId).IsCancelled.Should().BeTrue();
    }
}
