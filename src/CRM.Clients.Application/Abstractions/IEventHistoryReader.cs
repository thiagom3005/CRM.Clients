using CRM.Clients.Application.Customers.Queries.Models;

namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Contrato de leitura do historico de eventos de um aggregate.
/// Separado do IEventStore para respeitar o principio de segregacao de interfaces.
/// </summary>
public interface IEventHistoryReader
{
    /// <summary>
    /// Retorna uma pagina do historico de eventos de um aggregate ordenada por versao crescente.
    /// </summary>
    Task<PagedResult<CustomerEventDto>> GetPagedAsync(
        Guid aggregateId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
