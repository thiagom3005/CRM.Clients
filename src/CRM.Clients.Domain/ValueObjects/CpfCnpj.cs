using System.Text.RegularExpressions;
using CRM.Clients.Domain.Exceptions;

namespace CRM.Clients.Domain.ValueObjects;

/// <summary>
/// Documento fiscal brasileiro: CPF (11 dígitos) ou CNPJ (14 dígitos).
/// Armazena apenas dígitos — pontuação é descartada na criação.
/// TODO: adicionar validação dos dígitos verificadores (Mod 11) em evolução futura.
/// </summary>
public sealed partial record CpfCnpj
{
    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitsRegex();

    public string Value { get; }

    /// <summary>Indica se o documento é CPF (PF) ou CNPJ (PJ).</summary>
    public bool IsCpf => Value.Length == 11;
    public bool IsCnpj => Value.Length == 14;

    private CpfCnpj(string value) => Value = value;

    public static CpfCnpj Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("CPF/CNPJ não pode ser vazio.");

        var digits = NonDigitsRegex().Replace(raw, string.Empty);

        if (digits.Length is not (11 or 14))
            throw new DomainException($"CPF/CNPJ inválido: esperado 11 (CPF) ou 14 (CNPJ) dígitos, recebido {digits.Length}.");

        return new CpfCnpj(digits);
    }

    public override string ToString() => Value;
}
