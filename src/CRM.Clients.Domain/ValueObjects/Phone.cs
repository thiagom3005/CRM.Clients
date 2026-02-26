using System.Text.RegularExpressions;
using CRM.Clients.Domain.Exceptions;

namespace CRM.Clients.Domain.ValueObjects;

/// <summary>
/// Numero de telefone normalizado para somente digitos.
/// Aceita de 10 a 13 digitos para contemplar numeros nacionais e internacionais com DDI.
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
            throw new DomainException("Telefone nao pode ser vazio.");
        }

        string digits = NonDigitsRegex().Replace(raw, string.Empty);

        if (digits.Length is < 10 or > 13)
        {
            throw new DomainException(
                $"Telefone invalido: esperado entre 10 e 13 digitos, recebido {digits.Length}.");
        }

        return new Phone(digits);
    }

    public override string ToString() => Value;
}
