using CRM.Clients.Domain.Aggregates.Customer;

namespace CRM.Clients.Application.Customers.Queries.Models;

/// <summary>
/// DTO resumido para listas paginadas.
/// Projeta apenas os campos necessarios para evitar transferir dados desnecessarios.
/// </summary>
public sealed record CustomerListItemDto(
    Guid Id,
    CustomerType Type,
    string Name,
    string Document,
    string Email,
    string City,
    string State,
    DateTimeOffset UpdatedAtUtc);
