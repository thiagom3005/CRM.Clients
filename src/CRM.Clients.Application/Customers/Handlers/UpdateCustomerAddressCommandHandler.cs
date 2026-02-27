using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CRM.Clients.Application.Customers.Handlers;

public sealed partial class UpdateCustomerAddressCommandHandler(
    IEventStore eventStore,
    ICustomerProjectionWriter projectionWriter,
    ICustomerReadModelReader readModelReader,
    IExecutionContextAccessor executionContext,
    ILogger<UpdateCustomerAddressCommandHandler> logger)
    : IRequestHandler<UpdateCustomerAddressCommand>
{
    public async Task Handle(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var history = await eventStore.LoadAsync(request.CustomerId, cancellationToken);

        if (history.Count == 0)
        {
            throw new NotFoundException($"Cliente {request.CustomerId} nao encontrado.");
        }

        Customer customer   = Customer.Rehydrate(history);
        int expectedVersion = customer.Version;

        var address = Address.Create(
            request.ZipCode, request.Street, request.Number,
            request.District, request.City, request.State);

        customer.UpdateAddress(address);

        var metadata = new EventMetadata(
            CorrelationId: executionContext.CorrelationId,
            UserId:        executionContext.UserId);

        await eventStore.AppendAsync(
            request.CustomerId,
            "Customer",
            expectedVersion,
            customer.DomainEvents,
            metadata,
            cancellationToken);

        await projectionWriter.ProjectAsync(request.CustomerId, customer.DomainEvents, cancellationToken);

        await readModelReader.SaveAsync(cancellationToken);

        LogAddressUpdated(logger, request.CustomerId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Endereco atualizado: {CustomerId}")]
    private static partial void LogAddressUpdated(ILogger logger, Guid customerId);
}
