using System.Text.RegularExpressions;
using CRM.Clients.Domain.Exceptions;

namespace CRM.Clients.Domain.ValueObjects;

/// <summary>
/// Número de telefone normalizado para somente dígitos.
/// Aceita de 10 a 13 dígitos para contemplar números nacionais e internacionais com DDI.
/// </summary>
public sealed partial record Phone
{
    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitsRegex();

    public string Value { get; }

    private Phone(string value)
    {
        Value = value;
    }

    public static Phone Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainException("Telefone não pode ser vazio.");
        }

        string digits = NonDigitsRegex().Replace(raw, string.Empty);

        return digits.Length is < 10 or > 13
            ? throw new DomainException($"Telefone inválido: esperado entre 10 e 13 dígitos, recebido {digits.Length}.")
            : new Phone(digits);
    }

    public override string ToString() => Value;
}
