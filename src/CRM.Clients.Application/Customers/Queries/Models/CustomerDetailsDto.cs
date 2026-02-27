using CRM.Clients.Domain.Aggregates.Customer;

namespace CRM.Clients.Application.Customers.Queries.Models;

/// <summary>DTO completo retornado pelo GET /customers/{id}.</summary>
public sealed record CustomerDetailsDto(
    Guid Id,
    CustomerType Type,
    string Name,
    string Document,
    DateOnly BirthOrFoundationDate,
    string Email,
    string Phone,
    string ZipCode,
    string Street,
    string Number,
    string District,
    string City,
    string State,
    string? StateRegistration,
    bool IsStateRegistrationExempt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
