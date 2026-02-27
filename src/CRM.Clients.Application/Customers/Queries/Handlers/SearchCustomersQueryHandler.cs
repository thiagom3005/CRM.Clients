using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries.Handlers;

public sealed class SearchCustomersQueryHandler(ICustomerReadRepository readRepository)
    : IRequestHandler<SearchCustomersQuery, PagedResult<CustomerListItemDto>>
{
    private const int MaxPageSize = 100;

    public Task<PagedResult<CustomerListItemDto>> Handle(
        SearchCustomersQuery query, CancellationToken cancellationToken)
    {
        // Clamp defensivo: evita abusos de pageSize antes de atingir o banco.
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        return readRepository.SearchAsync(query.Search, page, pageSize, query.Sort, cancellationToken);
    }
}
