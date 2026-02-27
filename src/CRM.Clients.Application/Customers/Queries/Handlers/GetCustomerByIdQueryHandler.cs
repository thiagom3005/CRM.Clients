using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries.Handlers;

public sealed class GetCustomerByIdQueryHandler(ICustomerReadRepository readRepository)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDetailsDto>
{
    public async Task<CustomerDetailsDto> Handle(
        GetCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        var dto = await readRepository.GetByIdAsync(query.Id, cancellationToken);

        return dto ?? throw new NotFoundException($"Cliente {query.Id} nao encontrado.");
    }
}
