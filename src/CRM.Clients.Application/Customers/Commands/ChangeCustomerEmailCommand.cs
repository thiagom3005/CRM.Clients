using MediatR;

namespace CRM.Clients.Application.Customers.Commands;

/// <summary>
/// Altera o e-mail de um cliente existente.
/// </summary>
public sealed record ChangeCustomerEmailCommand(
    Guid CustomerId,
    string Email) : IRequest;
