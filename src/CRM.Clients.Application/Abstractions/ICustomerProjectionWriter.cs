using CRM.Clients.Domain.Events;

namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Atualiza o read model de cliente a partir dos novos eventos gerados.
/// Opera no mesmo contexto de banco da transacao corrente (sem SaveChanges proprio).
/// </summary>
public interface ICustomerProjectionWriter
{
    Task ProjectAsync(
        Guid aggregateId,
        IReadOnlyCollection<IDomainEvent> newEvents,
        CancellationToken ct = default);
}
