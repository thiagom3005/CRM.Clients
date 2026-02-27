using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries;

/// <summary>Retorna o detalhamento completo de um cliente pelo Id.</summary>
public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailsDto>;
