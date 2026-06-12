using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Ambev.DeveloperEvaluation.Functional.Infrastructure;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Functional tests for /api/Sales.
///
/// Errors caught historically:
/// - saleNumber returned random hash (handler ignored command.SaleNumber)
/// - items: [] in response (SaleCreatedEvent had no items; GetPagedAsync missing Include)
/// - 'Date deve ser informado' (date was required in request)
/// - quantity > 20 returned 500 instead of 422 with message
/// - GET /api/Sales/{id} returned 200 with wrong body when sale didn't exist in read model
/// </summary>
public class SalesFunctionalTests : FunctionalTestBase
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public SalesFunctionalTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── POST /api/Sales ───────────────────────────────────────────────────────

    [Fact(DisplayName = "POST /api/Sales: valid payload returns 201 with items and discounts")]
    public async Task CreateSale_ValidPayload_Returns201WithItemsAndDiscounts()
    {
        var token = await CreateUserAndGetTokenAsync("salespost@functional.com");
        SetBearerToken(token);

        // When — 5 items → 10% discount
        var response = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-FUNC-001", quantity: 5));

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: await response.Content.ReadAsStringAsync());

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");

        data.GetProperty("saleNumber").GetString().Should().Be("VND-FUNC-001",
            "saleNumber must reflect what was sent — historically handler ignored it and returned a random hash");

        // date must NOT be in the request (server sets it) but must be in the response
        data.TryGetProperty("date", out _).Should().BeTrue("date must be set by server and returned");

        var items = data.GetProperty("items");
        items.GetArrayLength().Should().Be(1,
            "items must be returned — historically items was always []");

        var item = items[0];
        item.GetProperty("discount").GetDecimal().Should().Be(0.10m,
            "5 units must trigger 10% discount");
        item.GetProperty("totalAmount").GetDecimal().Should().Be(45m,
            "5 * 10 * 0.9 = 45");
        item.GetProperty("id").GetString().Should().NotBeNullOrEmpty(
            "item ID is needed to call PATCH /api/Sales/{id}/items/{itemId}/cancel");
    }

    [Fact(DisplayName = "POST /api/Sales: quantity > 20 returns 400 with descriptive message")]
    public async Task CreateSale_QuantityOver20_Returns400()
    {
        var token = await CreateUserAndGetTokenAsync("salesover20@functional.com");
        SetBearerToken(token);

        var response = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-OVER", quantity: 21));

        // FluentValidation (CreateSaleValidator) catches quantity > 20 before the handler runs → 400
        // The handler would return 422 (DomainException), but the validator fires first via ValidationBehavior
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "quantity > 20 is caught by FluentValidation before reaching domain logic");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().ContainAny("20", "Quantity", "quantity",
            "error message must mention the constraint");
    }

    [Fact(DisplayName = "POST /api/Sales: unauthenticated returns 401 with JSON body")]
    public async Task CreateSale_Unauthenticated_Returns401WithBody()
    {
        ClearBearerToken();

        var response = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-NOAUTH"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeEmpty("401 must return a JSON body");
        JsonDocument.Parse(body).RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "POST /api/Sales: missing items returns 400")]
    public async Task CreateSale_MissingItems_Returns400()
    {
        var token = await CreateUserAndGetTokenAsync("salesnullitem@functional.com");
        SetBearerToken(token);

        var response = await Client.PostAsJsonAsync("/api/Sales", new
        {
            saleNumber = "VND-NITEM",
            customerId = CustomerId,
            customerName = "Cliente",
            branchId = BranchId,
            branchName = "Filial"
            // items omitted
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/Sales: no discount for quantity < 4")]
    public async Task CreateSale_QuantityBelow4_NoDiscount()
    {
        var token = await CreateUserAndGetTokenAsync("salesnodisc@functional.com");
        SetBearerToken(token);

        var response = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-NODISC", quantity: 3));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var data = (await ReadDataAsync<JsonElement>(response)).ValueKind == JsonValueKind.Undefined
            ? default : await ReadDataAsync<JsonElement>(response);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = body.RootElement.GetProperty("data").GetProperty("items")[0];
        item.GetProperty("discount").GetDecimal().Should().Be(0m);
        item.GetProperty("totalAmount").GetDecimal().Should().Be(30m, "3 * 10 * 1.0 = 30");
    }

    [Fact(DisplayName = "POST /api/Sales: 20% discount for quantity 10-20")]
    public async Task CreateSale_Quantity10_Gets20PercentDiscount()
    {
        var token = await CreateUserAndGetTokenAsync("sales20pct@functional.com");
        SetBearerToken(token);

        var response = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-20PCT", quantity: 10));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = body.RootElement.GetProperty("data").GetProperty("items")[0];
        item.GetProperty("discount").GetDecimal().Should().Be(0.20m);
        item.GetProperty("totalAmount").GetDecimal().Should().Be(80m, "10 * 10 * 0.8 = 80");
    }

    // ── GET /api/Sales/{id} ───────────────────────────────────────────────────

    [Fact(DisplayName = "GET /api/Sales/{id}: returns sale with items populated")]
    public async Task GetSale_ExistingSale_ReturnsSaleWithItems()
    {
        var token = await CreateUserAndGetTokenAsync("salesget@functional.com");
        SetBearerToken(token);

        // Create
        var createResp = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-GET", quantity: 5));
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Get
        var response = await Client.GetAsync($"/api/Sales/{saleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        data.GetProperty("saleNumber").GetString().Should().Be("VND-GET");
        data.GetProperty("items").GetArrayLength().Should().Be(1,
            "GET /api/Sales/{id} must return items — historically returned empty array");
    }

    [Fact(DisplayName = "GET /api/Sales/{id}: unknown id returns 404")]
    public async Task GetSale_UnknownId_Returns404()
    {
        var token = await CreateUserAndGetTokenAsync("salesnotfound@functional.com");
        SetBearerToken(token);

        var response = await Client.GetAsync($"/api/Sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/Sales ────────────────────────────────────────────────────────

    [Fact(DisplayName = "GET /api/Sales: returns paginated list")]
    public async Task ListSales_Returns200WithPaginatedData()
    {
        var token = await CreateUserAndGetTokenAsync("saleslist@functional.com");
        SetBearerToken(token);

        var response = await Client.GetAsync("/api/Sales?_page=1&_size=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("data").TryGetProperty("currentPage", out _).Should().BeTrue();
        body.RootElement.GetProperty("data").TryGetProperty("data", out _).Should().BeTrue();
    }

    // ── DELETE /api/Sales/{id} ────────────────────────────────────────────────

    [Fact(DisplayName = "DELETE /api/Sales/{id}: soft-deletes sale")]
    public async Task DeleteSale_ExistingSale_Returns200AndMarksCancelled()
    {
        var token = await CreateUserAndGetTokenAsync("salesdel@functional.com");
        SetBearerToken(token);

        var createResp = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-DEL"));
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Delete
        var deleteResp = await Client.DeleteAsync($"/api/Sales/{saleId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify isCancelled = true
        var getResp = await Client.GetAsync($"/api/Sales/{saleId}");
        var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("data").GetProperty("isCancelled").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "DELETE /api/Sales/{id}: unknown id returns 404")]
    public async Task DeleteSale_UnknownId_Returns404()
    {
        var token = await CreateUserAndGetTokenAsync("salesdelnotfound@functional.com");
        SetBearerToken(token);

        var response = await Client.DeleteAsync($"/api/Sales/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/Sales/{id}/items/{itemId}/cancel ───────────────────────────

    [Fact(DisplayName = "PATCH cancel item: marks item as cancelled")]
    public async Task CancelSaleItem_ExistingItem_Returns200AndMarksItemCancelled()
    {
        var token = await CreateUserAndGetTokenAsync("salescancelitem@functional.com");
        SetBearerToken(token);

        // Create sale with 1 item
        var createResp = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-CANCEL-ITEM"));
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        var itemId = createDoc.RootElement.GetProperty("data")
            .GetProperty("items")[0].GetProperty("id").GetString();

        // Cancel item
        var response = await Client.PatchAsync($"/api/Sales/{saleId}/items/{itemId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: await response.Content.ReadAsStringAsync());

        // Verify via GET
        var getDoc = JsonDocument.Parse(await (await Client.GetAsync($"/api/Sales/{saleId}")).Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("data").GetProperty("items")[0]
            .GetProperty("isCancelled").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "PATCH cancel item: already-cancelled item returns 422")]
    public async Task CancelSaleItem_AlreadyCancelled_Returns422()
    {
        var token = await CreateUserAndGetTokenAsync("salescanceltwice@functional.com");
        SetBearerToken(token);

        var createResp = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-CANCEL-TWICE"));
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        var itemId = createDoc.RootElement.GetProperty("data")
            .GetProperty("items")[0].GetProperty("id").GetString();

        await Client.PatchAsync($"/api/Sales/{saleId}/items/{itemId}/cancel", null);
        var response = await Client.PatchAsync($"/api/Sales/{saleId}/items/{itemId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "PATCH cancel item on cancelled sale returns 422")]
    public async Task CancelSaleItem_OnCancelledSale_Returns422()
    {
        var token = await CreateUserAndGetTokenAsync("salescancelonsale@functional.com");
        SetBearerToken(token);

        var createResp = await Client.PostAsJsonAsync("/api/Sales", BuildCreatePayload("VND-CANCEL-SALE"));
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var saleId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        var itemId = createDoc.RootElement.GetProperty("data")
            .GetProperty("items")[0].GetProperty("id").GetString();

        await Client.DeleteAsync($"/api/Sales/{saleId}");

        var response = await Client.PatchAsync($"/api/Sales/{saleId}/items/{itemId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static object BuildCreatePayload(string saleNumber, int quantity = 1) => new
    {
        saleNumber,
        customerId = CustomerId,
        customerName = "Cliente Funcional",
        branchId = BranchId,
        branchName = "Filial SP",
        items = new[]
        {
            new
            {
                productId = ProductId,
                productTitle = "Cerveja Brahma 350ml",
                quantity,
                unitPrice = 10m
            }
        }
    };
}
