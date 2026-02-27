using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries;

/// <summary>
/// Handler que lê o historico paginado de eventos usando IEventHistoryReader.
/// Lança NotFoundException se o aggregate nao possui nenhum evento registrado.
/// </summary>
public sealed class GetCustomerEventsQueryHandler(IEventHistoryReader historyReader)
    : IRequestHandler<GetCustomerEventsQuery, PagedResult<CustomerEventDto>>
{
    public async Task<PagedResult<CustomerEventDto>> Handle(
        GetCustomerEventsQuery request,
        CancellationToken cancellationToken)
    {
        int page     = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        PagedResult<CustomerEventDto> result =
            await historyReader.GetPagedAsync(request.CustomerId, page, pageSize, cancellationToken);

        if (result.Total == 0)
        {
            throw new NotFoundException($"Cliente {request.CustomerId} nao encontrado.");
        }

        return result;
    }
}
