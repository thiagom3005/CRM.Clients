using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Queries.Models;
using CRM.Clients.Infrastructure.ExternalServices;

namespace CRM.Clients.IntegrationTests.ExternalServices;

/// <summary>
/// Testes unitarios do ViaCepClient usando FakeHttpMessageHandler.
/// Nao usa Testcontainers nem WebApplicationFactory — apenas HttpClient simulado.
/// </summary>
public sealed class ViaCepClientTests
{
    // -------------------------------------------------------------------------
    // Fake handler
    // -------------------------------------------------------------------------

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string jsonBody)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Servico indisponivel.");
    }

    private static ViaCepClient BuildClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://viacep.com.br/")
        };
        return new ViaCepClient(httpClient);
    }

    // -------------------------------------------------------------------------
    // Testes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAddressAsync_ValidCep_ReturnsFilledDto()
    {
        // Arrange
        const string responseJson = """
            {
              "cep": "01310-100",
              "logradouro": "Avenida Paulista",
              "bairro": "Bela Vista",
              "localidade": "São Paulo",
              "uf": "SP"
            }
            """;

        ViaCepClient client = BuildClient(
            new FakeHttpMessageHandler(HttpStatusCode.OK, responseJson));

        // Act
        ViaCepAddressDto? dto = await client.GetAddressAsync("01310100");

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("01310-100", dto.ZipCode);
        Assert.Equal("Avenida Paulista", dto.Street);
        Assert.Equal("Bela Vista", dto.District);
        Assert.Equal("São Paulo", dto.City);
        Assert.Equal("SP", dto.State);
    }

    [Fact]
    public async Task GetAddressAsync_CepNotFound_ReturnsNull()
    {
        // ViaCEP retorna { "erro": true } quando o CEP nao existe.
        const string responseJson = """{ "erro": true }""";

        ViaCepClient client = BuildClient(
            new FakeHttpMessageHandler(HttpStatusCode.OK, responseJson));

        // Act
        ViaCepAddressDto? dto = await client.GetAddressAsync("99999999");

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public async Task GetAddressAsync_HttpError_ThrowsServiceUnavailableException()
    {
        // Arrange
        ViaCepClient client = BuildClient(new ThrowingHttpMessageHandler());

        // Act & Assert
        await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => client.GetAddressAsync("01310100"));
    }

    [Fact]
    public async Task GetAddressAsync_ServerError_ThrowsServiceUnavailableException()
    {
        // Arrange -- simula HTTP 500 do servidor ViaCEP
        ViaCepClient client = BuildClient(
            new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}"));

        // Act & Assert -- HttpClient lanca HttpRequestException para respostas de erro
        await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => client.GetAddressAsync("01310100"));
    }
}
