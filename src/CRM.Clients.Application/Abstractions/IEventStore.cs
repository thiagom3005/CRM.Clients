using CRM.Clients.Domain.Events;

namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Contrato do event store. Isola a persistencia de eventos da logica de aplicacao.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Carrega todos os eventos de um aggregate em ordem de versao.
    /// Retorna lista vazia se o aggregate nao existir.
    /// </summary>
    Task<IReadOnlyList<IDomainEvent>> LoadAsync(
        Guid aggregateId,
        CancellationToken ct = default);

    /// <summary>
    /// Registra novos eventos no event store.
    /// Lanca <see cref="Common.Exceptions.ConcurrencyException"/> se expectedVersion divergir.
    /// </summary>
    Task AppendAsync(
        Guid aggregateId,
        string aggregateType,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        EventMetadata metadata,
        CancellationToken ct = default);
}
