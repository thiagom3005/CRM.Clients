using System.Text.RegularExpressions;
using CRM.Clients.Domain.Exceptions;

namespace CRM.Clients.Domain.ValueObjects;

/// <summary>
/// Endereco completo do cliente. CEP normalizado para digitos; UF em maiusculas.
/// Consulta de CEP (ViaCEP) e responsabilidade da camada Application/Infrastructure.
/// </summary>
public sealed partial record Address
{
    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitsRegex();

    public string ZipCode { get; }

    public string Street { get; }

    public string Number { get; }

    public string District { get; }

    public string City { get; }

    /// <summary>UF com exatamente 2 letras maiusculas (ex.: SP, RJ).</summary>
    public string State { get; }

    private Address(
        string zipCode,
        string street,
        string number,
        string district,
        string city,
        string state)
    {
        ZipCode = zipCode;
        Street = street;
        Number = number;
        District = district;
        City = city;
        State = state;
    }

    public static Address Create(
        string zipCode,
        string street,
        string number,
        string district,
        string city,
        string state)
    {
        string zip = NonDigitsRegex().Replace(zipCode ?? string.Empty, string.Empty);

        if (zip.Length != 8)
        {
            throw new DomainException(
                $"CEP invalido: esperado 8 digitos, recebido {zip.Length}.");
        }

        if (string.IsNullOrWhiteSpace(street))
        {
            throw new DomainException("Logradouro nao pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("Cidade nao pode ser vazia.");
        }

        string uf = state?.Trim().ToUpperInvariant() ?? string.Empty;

        if (uf.Length != 2)
        {
            throw new DomainException(
                $"UF invalida: esperado 2 letras, recebido '{uf}'.");
        }

        return new Address(
            zip,
            street.Trim(),
            number?.Trim() ?? "S/N",
            district?.Trim() ?? string.Empty,
            city.Trim(),
            uf);
    }

    public override string ToString() =>
        $"{Street}, {Number} -- {District}, {City}/{State} -- {ZipCode}";
}
