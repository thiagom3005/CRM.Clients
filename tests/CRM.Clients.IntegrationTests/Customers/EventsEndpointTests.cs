using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Clients.Application.Customers.Queries.Models;
using CRM.Clients.IntegrationTests.Fixtures;

namespace CRM.Clients.IntegrationTests.Customers;

/// <summary>
/// Testes do endpoint GET /customers/{id}/events.
/// Usa container Postgres dedicado para esta classe (IClassFixture).
/// CPF 061.523.027-06 e valido (verificado pelo algoritmo).
/// </summary>
public sealed class EventsEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateCustomerAsync(string document, string email)
    {
        var payload = new
        {
            type                   = "Individual",
            name                   = "Cliente Eventos",
            document,
            birthOrFoundationDate  = "1985-05-10",
            email,
            phone                  = "11988887777",
            zipCode                = "01310100",
            street                 = "Av. Paulista",
            number                 = "1000",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = false
        };

        HttpResponseMessage r = await fixture.Client.PostAsJsonAsync("/customers", payload);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<Guid>();
    }

    // -------------------------------------------------------------------------
    // Testes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEvents_ExistingCustomer_Returns200WithAtLeastOneEvent()
    {
        // Arrange
        Guid id = await CreateCustomerAsync(
            "061.523.027-06",
            $"eventos.{Guid.NewGuid():N}@example.com");

        // Act
        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers/{id}/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        int total = root.GetProperty("total").GetInt32();
        Assert.True(total >= 1, "Deve haver pelo menos 1 evento apos criacao do cliente.");

        JsonElement items = root.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);

        JsonElement first = items[0];
        Assert.True(first.TryGetProperty("eventType", out _), "Item deve ter 'eventType'.");
        Assert.True(first.TryGetProperty("version", out _),   "Item deve ter 'version'.");
        Assert.True(first.TryGetProperty("data", out _),      "Item deve ter 'data'.");
    }

    [Fact]
    public async Task GetEvents_NonExistentCustomer_Returns404()
    {
        // Arrange
        Guid nonExistent = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers/{nonExistent}/events");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_WithCorrelationIdHeader_CorrelationIdPresentInEventMetadata()
    {
        // Arrange
        string correlationId = $"test-{Guid.NewGuid():N}";

        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "/customers");
        requestMsg.Headers.Add("X-Correlation-Id", correlationId);
        requestMsg.Headers.Add("X-User", "test-user");

        var payload = new
        {
            type                   = "Individual",
            name                   = "Cliente Correlacao",
            document               = "853.726.014-72",
            birthOrFoundationDate  = "1992-08-22",
            email                  = $"corr.{Guid.NewGuid():N}@example.com",
            phone                  = "11977776666",
            zipCode                = "01310100",
            street                 = "Av. Paulista",
            number                 = "500",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = false
        };

        requestMsg.Content = JsonContent.Create(payload);
        HttpResponseMessage createResp = await fixture.Client.SendAsync(requestMsg);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        Guid id = await createResp.Content.ReadFromJsonAsync<Guid>();

        // Act
        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers/{id}/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PagedResult<CustomerEventDto>? result =
            await response.Content.ReadFromJsonAsync<PagedResult<CustomerEventDto>>(JsonOpts);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        CustomerEventDto firstEvent = result.Items[0];
        Assert.Equal(correlationId, firstEvent.CorrelationId);
        Assert.Equal("test-user", firstEvent.UserId);
    }

    [Fact]
    public async Task GetEvents_SecondPage_ReturnsCorrectSlice()
    {
        // Arrange -- cria cliente e gera 2 eventos (create + changeEmail)
        Guid id = await CreateCustomerAsync(
            "111.444.777-35",
            $"pag.{Guid.NewGuid():N}@example.com");

        // Gera segundo evento (email change)
        var changePayload = new { email = $"pag2.{Guid.NewGuid():N}@example.com" };
        HttpResponseMessage changeResp = await fixture.Client
            .PutAsJsonAsync($"/customers/{id}/email", changePayload);
        Assert.Equal(HttpStatusCode.NoContent, changeResp.StatusCode);

        // Act -- pede pagina 2 com pageSize=1 (deve retornar o segundo evento)
        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers/{id}/events?page=2&pageSize=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        Assert.Equal(2, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, root.GetProperty("items").GetArrayLength());
    }
}
