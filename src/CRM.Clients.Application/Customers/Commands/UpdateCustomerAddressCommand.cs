using MediatR;

namespace CRM.Clients.Application.Customers.Commands;

/// <summary>
/// Atualiza o endereco de um cliente existente.
/// </summary>
public sealed record UpdateCustomerAddressCommand(
    Guid CustomerId,
    string ZipCode,
    string Street,
    string Number,
    string District,
    string City,
    string State) : IRequest;
