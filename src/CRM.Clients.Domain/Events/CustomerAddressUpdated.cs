namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando o endereco do cliente e atualizado.
/// Carrega todos os campos do endereco para rehydration.
/// </summary>
public sealed record CustomerAddressUpdated(
    Guid CustomerId,
    string ZipCode,
    string Street,
    string Number,
    string District,
    string City,
    string State,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
