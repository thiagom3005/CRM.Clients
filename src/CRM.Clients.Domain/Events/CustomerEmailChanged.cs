namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando o e-mail do cliente é alterado.
/// Carrega o novo valor (já normalizado) para possível reindexação/notificação.
/// </summary>
public sealed record CustomerEmailChanged(
    Guid CustomerId,
    string NewEmail,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
