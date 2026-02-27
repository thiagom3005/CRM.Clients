using System.Text.Json.Serialization;

namespace CRM.Clients.Infrastructure.ExternalServices;

/// <summary>
/// Modelo interno que espelha o JSON retornado pela API ViaCEP.
/// Nao e exposto fora da Infrastructure — mapeado para ViaCepAddressDto no cliente.
/// </summary>
internal sealed class ViaCepResponse
{
    [JsonPropertyName("cep")]
    public string? Cep { get; init; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; init; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; init; }

    [JsonPropertyName("localidade")]
    public string? Localidade { get; init; }

    [JsonPropertyName("uf")]
    public string? Uf { get; init; }

    /// <summary>
    /// ViaCEP retorna <c>true</c> (booleano JSON) quando o CEP nao e encontrado.
    /// </summary>
    [JsonPropertyName("erro")]
    public bool? Erro { get; init; }
}
