using System.Net;
using System.Net.Http.Json;
using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.IntegrationTests.Fixtures;

namespace CRM.Clients.IntegrationTests.Customers;

/// <summary>
/// Testes end-to-end do fluxo CreateCustomer:
/// HTTP POST -> Handler -> EventStore -> Projection -> Postgres real (Testcontainers).
/// </summary>
public sealed class CreateCustomerTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task PostCustomer_ValidIndividual_Returns201WithId()
    {
        // Arrange
        var payload = new
        {
            type = (int)CustomerType.Individual,
            name = "Joao Silva",
            document = "529.982.247-25",
            birthOrFoundationDate = "1990-06-15",
            email = $"joao.silva.{Guid.NewGuid():N}@example.com",
            phone = "11987654321",
            zipCode = "01001000",
            street = "Praca da Se",
            number = "100",
            district = "Se",
            city = "Sao Paulo",
            state = "SP",
            stateRegistration = (string?)null,
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
            type = (int)CustomerType.Individual,
            name = "Maria Souza",
            document = "853.726.014-72",
            birthOrFoundationDate = "1985-03-20",
            email,
            phone = "11912345678",
            zipCode = "01001000",
            street = "Av. Paulista",
            number = "1000",
            district = "Bela Vista",
            city = "Sao Paulo",
            state = "SP",
            stateRegistration = (string?)null,
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
            type = (int)CustomerType.Individual,
            name = "Adolescente",
            document = "529.982.247-25",
            birthOrFoundationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)).ToString("yyyy-MM-dd"),
            email = $"adolescente.{Guid.NewGuid():N}@example.com",
            phone = "11999999999",
            zipCode = "01001000",
            street = "Rua X",
            number = "1",
            district = "Centro",
            city = "SP",
            state = "SP",
            stateRegistration = (string?)null,
            isStateRegistrationExempt = false
        };

        // Act
        HttpResponseMessage response = await fixture.Client
            .PostAsJsonAsync("/customers", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
