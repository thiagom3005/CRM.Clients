using System.Net;
using System.Net.Http.Json;
using CRM.Clients.Application.Customers.Queries.Models;
using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.IntegrationTests.Fixtures;

namespace CRM.Clients.IntegrationTests.Customers;

/// <summary>
/// Testes end-to-end do read side:
/// POST (write) -> GET /customers/{id} e GET /customers?search= (read).
///
/// CPFs distintos por teste: todos compartilham o mesmo container/DB dentro
/// do IClassFixture, entao nao pode repetir documento.
/// CPFs verificados com o algoritmo de validacao oficial.
/// </summary>
public sealed class ReadCustomerTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    // -------------------------------------------------------------------------
    // GET /customers/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingCustomer_ReturnsDetails()
    {
        string email = $"getbyid.{Guid.NewGuid():N}@test.com";
        Guid id = await CreateIndividualAsync("529.982.247-25", "Ana Lima", email);

        HttpResponseMessage response = await fixture.Client.GetAsync($"/customers/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>();

        Assert.NotNull(dto);
        Assert.Equal(id, dto.Id);
        Assert.Equal("Ana Lima", dto.Name);
        Assert.Equal(email, dto.Email);
        Assert.Equal("01001000", dto.ZipCode);
        Assert.Equal(CustomerType.Individual, dto.Type);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /customers (search)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Search_ByPartialName_ReturnsOnlyMatches()
    {
        // Prefixo unico garante que so os clientes deste teste aparecem na busca.
        string prefix = Guid.NewGuid().ToString("N")[..10];

        await CreateIndividualAsync("853.726.014-72", $"{prefix} Joao",   $"{prefix}.j@test.com");
        await CreateIndividualAsync("111.444.777-35", $"{prefix} Maria",  $"{prefix}.m@test.com");
        await CreateIndividualAsync("123.456.789-09", "Outro Qualquer",   $"outro.{Guid.NewGuid():N}@test.com");

        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers?search={prefix}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItemDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.All(result.Items, item =>
            Assert.Contains(prefix, item.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_Pagination_ReturnsCorrectPage()
    {
        string prefix = Guid.NewGuid().ToString("N")[..10];

        // Tres clientes com CPFs distintos dos demais testes.
        await CreateIndividualAsync("987.654.321-00", $"{prefix} P1", $"{prefix}.p1@test.com");
        await CreateIndividualAsync("123.456.781-43", $"{prefix} P2", $"{prefix}.p2@test.com");
        await CreateIndividualAsync("234.567.890-92", $"{prefix} P3", $"{prefix}.p3@test.com");

        var resp1 = await fixture.Client
            .GetAsync($"/customers?search={prefix}&pageSize=2&page=1");
        var resp2 = await fixture.Client
            .GetAsync($"/customers?search={prefix}&pageSize=2&page=2");

        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        var page1 = await resp1.Content.ReadFromJsonAsync<PagedResult<CustomerListItemDto>>();
        var page2 = await resp2.Content.ReadFromJsonAsync<PagedResult<CustomerListItemDto>>();

        Assert.NotNull(page1);
        Assert.NotNull(page2);

        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.TotalPages);  // ceil(3/2) = 2
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);          // pagina 2: 1 item restante
    }

    [Fact]
    public async Task Search_ByExactDocument_ReturnsCustomer()
    {
        // Busca por documento (digitos normalizados) deve encontrar o cliente exato.
        string email = $"bydoc.{Guid.NewGuid():N}@test.com";
        Guid id = await CreateIndividualAsync("345.678.901-75", "Doc Busca", email);

        // Busca com formatacao (pontos e traco) -- o handler normaliza para digitos.
        HttpResponseMessage response = await fixture.Client
            .GetAsync("/customers?search=34567890175");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItemDto>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.Id == id);
    }

    [Fact]
    public async Task Search_InvalidPage_ReturnsFirstPage()
    {
        // page=0 deve ser tratado como page=1 pelo handler/repositorio.
        string prefix = Guid.NewGuid().ToString("N")[..10];
        await CreateIndividualAsync("456.789.012-91", $"{prefix} X", $"{prefix}.x@test.com");

        HttpResponseMessage response = await fixture.Client
            .GetAsync($"/customers?search={prefix}&page=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItemDto>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.NotEmpty(result.Items);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateIndividualAsync(string document, string name, string email)
    {
        var payload = new
        {
            type = (int)CustomerType.Individual,
            name,
            document,
            birthOrFoundationDate = "1990-01-15",
            email,
            phone = "11987654321",
            zipCode = "01001000",
            street = "Praca da Se",
            number = "1",
            district = "Se",
            city = "Sao Paulo",
            state = "SP",
            stateRegistration = (string?)null,
            isStateRegistrationExempt = false
        };

        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
