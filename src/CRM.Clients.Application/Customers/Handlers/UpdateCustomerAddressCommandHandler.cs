using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CRM.Clients.Application.Customers.Handlers;

public sealed class UpdateCustomerAddressCommandHandler(
    IEventStore eventStore,
    ICustomerProjectionWriter projectionWriter,
    ICustomerReadModelReader readModelReader,
    ILogger<UpdateCustomerAddressCommandHandler> logger)
    : IRequestHandler<UpdateCustomerAddressCommand>
{
    public async Task Handle(UpdateCustomerAddressCommand cmd, CancellationToken ct)
    {
        var history = await eventStore.LoadAsync(cmd.CustomerId, ct);

        if (history.Count == 0)
        {
            throw new NotFoundException($"Cliente {cmd.CustomerId} nao encontrado.");
        }

        Customer customer = Customer.Rehydrate(history);
        int expectedVersion = customer.Version;

        var address = Address.Create(
            cmd.ZipCode, cmd.Street, cmd.Number, cmd.District, cmd.City, cmd.State);

        customer.UpdateAddress(address);

        await eventStore.AppendAsync(
            cmd.CustomerId,
            "Customer",
            expectedVersion,
            customer.DomainEvents,
            EventMetadata.Empty,
            ct);

        await projectionWriter.ProjectAsync(cmd.CustomerId, customer.DomainEvents, ct);

        await readModelReader.SaveAsync(ct);

        logger.LogInformation(
            "Endereco atualizado: {CustomerId}", cmd.CustomerId);
    }
}
