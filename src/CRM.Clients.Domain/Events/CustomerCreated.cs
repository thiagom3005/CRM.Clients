using CRM.Clients.Domain.Aggregates.Customer;

namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando um novo cliente é criado no sistema.
/// </summary>
public sealed record CustomerCreated(
    Guid CustomerId,
    CustomerType Type,
    string Document,
    string Email,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
