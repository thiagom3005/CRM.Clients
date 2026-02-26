namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando o endereço do cliente é atualizado.
/// </summary>
public sealed record CustomerAddressUpdated(
    Guid CustomerId,
    string ZipCode,
    string City,
    string State,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
