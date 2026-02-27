using System.Text.RegularExpressions;
using CRM.Clients.Domain.Exceptions;

namespace CRM.Clients.Domain.ValueObjects;

/// <summary>
/// Documento fiscal brasileiro: CPF (11 digitos) ou CNPJ (14 digitos).
/// Armazena apenas digitos -- pontuacao e descartada na criacao.
/// TODO: adicionar validacao dos digitos verificadores (Mod 11) em evolucao futura.
/// </summary>
public sealed partial record CpfCnpj
{
    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitsRegex();

    public string Value { get; }

    /// <summary>Indica se o documento e CPF (PF) ou CNPJ (PJ).</summary>
    public bool IsCpf => Value.Length == 11;

    public bool IsCnpj => Value.Length == 14;

    private CpfCnpj(string value)
    {
        Value = value;
    }

    public static CpfCnpj Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainException("CPF/CNPJ nao pode ser vazio.");
        }

        string digits = NonDigitsRegex().Replace(raw, string.Empty);

        return digits.Length is not (11 or 14)
            ? throw new DomainException(
                $"CPF/CNPJ invalido: esperado 11 (CPF) ou 14 (CNPJ) digitos, recebido {digits.Length}.")
            : new CpfCnpj(digits);
    }

    public override string ToString() => Value;
}
