using System.Net.Http.Json;
using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Queries.Models;

namespace CRM.Clients.Infrastructure.ExternalServices;

/// <summary>
/// Implementacao de IViaCepClient que consulta https://viacep.com.br/ws/{cep}/json/.
/// O HttpClient injetado ja carrega as politicas Polly configuradas no DI (Retry + Timeout + CircuitBreaker).
/// </summary>
public sealed class ViaCepClient(HttpClient httpClient) : IViaCepClient
{
    public async Task<ViaCepAddressDto?> GetAddressAsync(string zipCode, CancellationToken ct = default)
    {
        ViaCepResponse? response;

        try
        {
            response = await httpClient.GetFromJsonAsync<ViaCepResponse>(
                $"ws/{zipCode}/json/", ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceUnavailableException(
                "O servico de consulta de CEP esta temporariamente indisponivel.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Timeout Polly ou HttpClient
            throw new ServiceUnavailableException(
                "A consulta de CEP excedeu o tempo limite.", ex);
        }

        if (response is null || response.Erro == true)
        {
            return null;
        }

        return new ViaCepAddressDto(
            ZipCode:  response.Cep  ?? zipCode,
            Street:   response.Logradouro ?? string.Empty,
            District: response.Bairro     ?? string.Empty,
            City:     response.Localidade ?? string.Empty,
            State:    response.Uf         ?? string.Empty);
    }
}
