using CRM.Clients.Domain.Aggregates.Customer;

namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando um novo cliente e criado no sistema.
/// Carrega todos os campos para rehydration do aggregate e projecao do read model.
/// </summary>
public sealed record CustomerCreated(
    Guid CustomerId,
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
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
