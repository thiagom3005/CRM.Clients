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
public sealed class ChangeCustomerEmailCommandHandler(
    IEventStore eventStore,
    ICustomerProjectionWriter projectionWriter,
    ICustomerReadModelReader readModelReader,
    ILogger<ChangeCustomerEmailCommandHandler> logger)
    : IRequestHandler<ChangeCustomerEmailCommand>
{
    public async Task Handle(ChangeCustomerEmailCommand cmd, CancellationToken ct)
    {
        var history = await eventStore.LoadAsync(cmd.CustomerId, ct);

        if (history.Count == 0)
        {
            throw new NotFoundException($"Cliente {cmd.CustomerId} nao encontrado.");
        }

        string normalizedEmail = cmd.Email.Trim().ToLowerInvariant();

        if (await readModelReader.EmailExistsAsync(normalizedEmail, ct))
        {
            throw new ConflictException("E-mail ja cadastrado.");
        }

        // Rehydrate: reconstroi estado sem gerar novos domain events.
        Customer customer = Customer.Rehydrate(history);
        int expectedVersion = customer.Version;

        customer.ChangeEmail(Email.Create(cmd.Email));

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
            "E-mail alterado: {CustomerId}", cmd.CustomerId);
    }
}
