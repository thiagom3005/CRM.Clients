using System.Net.Mail;
using CRM.Clients.Domain.Exceptions;

namespace CRM.Clients.Domain.ValueObjects;

/// <summary>
/// Endereco de e-mail normalizado (trim + lowercase).
/// Valida formato via <see cref="MailAddress"/> -- sem chamada externa.
/// </summary>
public sealed record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainException("E-mail nao pode ser vazio.");
        }

        string normalized = raw.Trim().ToLowerInvariant();

        try
        {
            // MailAddress valida o formato RFC 5321 de forma simples e sem regex fragil.
            _ = new MailAddress(normalized);
        }
        catch (FormatException)
        {
            throw new DomainException($"E-mail invalido: '{normalized}'.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
