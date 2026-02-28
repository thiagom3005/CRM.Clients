using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using CRM.Clients.IntegrationTests.Fixtures;

namespace CRM.Clients.IntegrationTests.Customers;

/// <summary>
/// Testes end-to-end do fluxo CreateCustomer:
/// HTTP POST -> Handler -> EventStore -> Projection -> Postgres real (Testcontainers).
///
/// O campo 'type' trafega como string ("Individual", "Company") conforme
/// JsonStringEnumConverter registrado globalmente na API.
/// </summary>
public sealed class CreateCustomerTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    // ── Pessoa Física ────────────────────────────────────────────────────────

    [Fact]
    public async Task PostCustomer_ValidIndividual_Returns201WithId()
    {
        // Arrange
        var payload = new
        {
            type                   = "Individual",
            name                   = "Joao Silva",
            document               = "529.982.247-25",
            birthOrFoundationDate  = "1990-06-15",
            email                  = $"joao.silva.{Guid.NewGuid():N}@example.com",
            phone                  = "11987654321",
            zipCode                = "01001000",
            street                 = "Praca da Se",
            number                 = "100",
            district               = "Se",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = false
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        Guid id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task PostCustomer_DuplicateEmail_Returns409()
    {
        // Arrange -- cria o primeiro cliente
        string email = $"dup.{Guid.NewGuid():N}@example.com";

        var payload = new
        {
            type                   = "Individual",
            name                   = "Maria Souza",
            document               = "853.726.014-72",
            birthOrFoundationDate  = "1985-03-20",
            email,
            phone                  = "11912345678",
            zipCode                = "01001000",
            street                 = "Av. Paulista",
            number                 = "1000",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = false
        };

        var first = await fixture.Client.PostAsJsonAsync("/customers", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Act -- tenta criar segundo com mesmo email mas CPF diferente
        var duplicate = payload with { document = "111.444.777-35" };
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", duplicate);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostCustomer_UnderageIndividual_Returns400()
    {
        // Arrange
        var payload = new
        {
            type                   = "Individual",
            name                   = "Adolescente",
            document               = "529.982.247-25",
            birthOrFoundationDate  = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17))
                                        .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            email                  = $"adolescente.{Guid.NewGuid():N}@example.com",
            phone                  = "11999999999",
            zipCode                = "01001000",
            street                 = "Rua X",
            number                 = "1",
            district               = "Centro",
            city                   = "SP",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = false
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Pessoa Jurídica ──────────────────────────────────────────────────────

    [Fact]
    public async Task PostCustomer_ValidCompanyWithIe_Returns201WithId()
    {
        // Arrange — nao isento exige IE preenchida
        var payload = new
        {
            type                   = "Company",
            name                   = "Empresa Teste Ltda",
            document               = "11222333000181",   // 14 digitos (CNPJ)
            birthOrFoundationDate  = "2000-01-01",
            email                  = $"empresa.ie.{Guid.NewGuid():N}@example.com",
            phone                  = "1133334444",
            zipCode                = "01310100",
            street                 = "Av. Paulista",
            number                 = "1000",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = "SP-123.456.789.000",
            isStateRegistrationExempt = false             // IE fornecida → OK
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Guid id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task PostCustomer_ValidCompanyIsento_Returns201WithId()
    {
        // Arrange — isento: IE nao deve ser preenchida
        var payload = new
        {
            type                   = "Company",
            name                   = "Isenta Comercio ME",
            document               = "44556677000195",   // 14 digitos (CNPJ)
            birthOrFoundationDate  = "2010-06-20",
            email                  = $"empresa.isento.{Guid.NewGuid():N}@example.com",
            phone                  = "1144445555",
            zipCode                = "01310100",
            street                 = "Av. Paulista",
            number                 = "500",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = true              // Isento + IE ausente → OK
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Guid id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task PostCustomer_CompanyMissingIeNotIsento_Returns400()
    {
        // Invariante: nao isento exige IE preenchida → DomainException → 400
        var payload = new
        {
            type                   = "Company",
            name                   = "Empresa Sem IE",
            document               = "77889900000164",   // 14 digitos (CNPJ)
            birthOrFoundationDate  = "2005-03-15",
            email                  = $"empresa.semie.{Guid.NewGuid():N}@example.com",
            phone                  = "1155556666",
            zipCode                = "01310100",
            street                 = "Av. Paulista",
            number                 = "200",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = (string?)null,
            isStateRegistrationExempt = false             // Nao isento + IE ausente → inválido
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCustomer_CompanyIeFilledButIsento_Returns400()
    {
        // Invariante: isento nao pode ter IE preenchida → DomainException → 400
        var payload = new
        {
            type                   = "Company",
            name                   = "Isento Com IE Preenchida",
            document               = "22334455000100",   // 14 digitos (CNPJ)
            birthOrFoundationDate  = "2015-08-10",
            email                  = $"empresa.ieisento.{Guid.NewGuid():N}@example.com",
            phone                  = "1166667777",
            zipCode                = "01310100",
            street                 = "Av. Paulista",
            number                 = "300",
            district               = "Bela Vista",
            city                   = "Sao Paulo",
            state                  = "SP",
            stateRegistration      = "SP-999.888.777.000",
            isStateRegistrationExempt = true              // Isento + IE preenchida → inválido
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
