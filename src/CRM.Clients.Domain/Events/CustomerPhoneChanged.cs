namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando o telefone de contato do cliente e alterado.
/// </summary>
public sealed record CustomerPhoneChanged(
    Guid CustomerId,
    string NewPhone,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
