using CRM.Clients.Application.Customers.Queries.Models;

namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Contrato para consulta de endereco via CEP em servico externo.
/// A implementacao concreta fica na Infrastructure (ViaCepClient).
/// </summary>
public interface IViaCepClient
{
    /// <summary>
    /// Retorna os dados de endereco para o <paramref name="zipCode"/> informado (somente digitos).
    /// Retorna <c>null</c> quando o CEP nao e encontrado (HTTP 200 com campo "erro": true).
    /// Lanca <see cref="Common.Exceptions.ServiceUnavailableException"/> apos esgotar as tentativas Polly.
    /// </summary>
    Task<ViaCepAddressDto?> GetAddressAsync(string zipCode, CancellationToken ct = default);
}
