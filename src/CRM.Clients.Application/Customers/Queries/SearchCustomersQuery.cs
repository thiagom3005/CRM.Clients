using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries;

/// <summary>
/// Busca paginada de clientes com filtro livre por nome, e-mail ou documento.
/// PageSize e limitado em 100 no handler para evitar queries gigantes.
/// </summary>
public sealed record SearchCustomersQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null) : IRequest<PagedResult<CustomerListItemDto>>;
