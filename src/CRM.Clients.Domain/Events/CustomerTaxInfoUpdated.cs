namespace CRM.Clients.Domain.Events;

/// <summary>
/// Disparado quando as informações tributárias da PJ são atualizadas (IE ou status de isenção).
/// </summary>
public sealed record CustomerTaxInfoUpdated(
    Guid CustomerId,
    string? StateRegistration,
    bool IsStateRegistrationExempt,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
