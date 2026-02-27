using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries;

/// <summary>
/// Retorna o historico paginado de eventos de um cliente.
/// </summary>
/// <param name="CustomerId">Identificador do aggregate.</param>
/// <param name="Page">Numero da pagina (base 1).</param>
/// <param name="PageSize">Tamanho da pagina (1-100).</param>
public sealed record GetCustomerEventsQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CustomerEventDto>>;
