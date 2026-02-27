using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CRM.Clients.Application.Customers.Handlers;

/// <summary>
/// Carrega historico de eventos, rehydrata o aggregate, aplica mutacao e persiste.
/// </summary>
public sealed partial class ChangeCustomerEmailCommandHandler(
    IEventStore eventStore,
    ICustomerProjectionWriter projectionWriter,
    ICustomerReadModelReader readModelReader,
    ILogger<ChangeCustomerEmailCommandHandler> logger)
    : IRequestHandler<ChangeCustomerEmailCommand>
{
    public async Task Handle(ChangeCustomerEmailCommand request, CancellationToken cancellationToken)
    {
        var history = await eventStore.LoadAsync(request.CustomerId, cancellationToken);

        if (history.Count == 0)
        {
            throw new NotFoundException($"Cliente {request.CustomerId} nao encontrado.");
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await readModelReader.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("E-mail ja cadastrado.");
        }

        // Rehydrate: reconstroi estado sem gerar novos domain events.
        Customer customer    = Customer.Rehydrate(history);
        int expectedVersion  = customer.Version;

        customer.ChangeEmail(Email.Create(request.Email));

        await eventStore.AppendAsync(
            request.CustomerId,
            "Customer",
            expectedVersion,
            customer.DomainEvents,
            EventMetadata.Empty,
            cancellationToken);

        await projectionWriter.ProjectAsync(request.CustomerId, customer.DomainEvents, cancellationToken);

        await readModelReader.SaveAsync(cancellationToken);

        LogEmailChanged(logger, request.CustomerId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "E-mail alterado: {CustomerId}")]
    private static partial void LogEmailChanged(ILogger logger, Guid customerId);
}
