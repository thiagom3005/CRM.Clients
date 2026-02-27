using CRM.Clients.Domain.Aggregates.Customer;
using MediatR;

namespace CRM.Clients.Application.Customers.Commands;

/// <summary>
/// Cria um novo cliente (PF ou PJ).
/// O tipo determina quais invariantes de negocio serao validadas pelo aggregate.
/// Retorna o Id do novo cliente.
/// </summary>
public sealed record CreateCustomerCommand(
    CustomerType Type,
    string Name,
    string Document,
    DateOnly BirthOrFoundationDate,
    string Email,
    string Phone,
    string ZipCode,
    string Street,
    string Number,
    string District,
    string City,
    string State,
    string? StateRegistration,
    bool IsStateRegistrationExempt) : IRequest<Guid>;
