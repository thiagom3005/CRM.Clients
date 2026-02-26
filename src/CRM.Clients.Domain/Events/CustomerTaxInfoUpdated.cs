namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando as informacoes tributarias da PJ sao atualizadas (IE ou status de isencao).
/// </summary>
public sealed record CustomerTaxInfoUpdated(
    Guid CustomerId,
    string? StateRegistration,
    bool IsStateRegistrationExempt,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
