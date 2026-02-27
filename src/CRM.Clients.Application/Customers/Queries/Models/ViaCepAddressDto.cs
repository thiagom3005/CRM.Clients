namespace CRM.Clients.Application.Customers.Queries.Models;

/// <summary>DTO retornado pelo endpoint GET /addresses/by-zipcode/{zipCode}.</summary>
public sealed record ViaCepAddressDto(
    string ZipCode,
    string Street,
    string District,
    string City,
    string State);
