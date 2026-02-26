namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando o e-mail do cliente e alterado.
/// Carrega o novo valor (ja normalizado) para possivel reindexacao/notificacao.
/// </summary>
public sealed record CustomerEmailChanged(
    Guid CustomerId,
    string NewEmail,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
