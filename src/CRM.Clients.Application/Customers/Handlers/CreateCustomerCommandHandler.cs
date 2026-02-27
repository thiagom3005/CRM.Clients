using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Domain.Abstractions;
using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CRM.Clients.Application.Customers.Handlers;

/// <summary>
/// Handler principal do write side.
/// Fluxo: validar unicidade -> criar aggregate -> append eventos -> projection -> commit.
/// </summary>
public sealed partial class CreateCustomerCommandHandler(
    IEventStore eventStore,
    ICustomerProjectionWriter projectionWriter,
    ICustomerReadModelReader readModelReader,
    IClock clock,
    IExecutionContextAccessor executionContext,
    ILogger<CreateCustomerCommandHandler> logger)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Normalizar antes de checar unicidade (o dominio faz o mesmo internamente).
        string normalizedDoc   = NormalizeDigits(request.Document);
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await readModelReader.DocumentExistsAsync(normalizedDoc, cancellationToken))
        {
            throw new ConflictException("CPF/CNPJ ja cadastrado.");
        }

        if (await readModelReader.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("E-mail ja cadastrado.");
        }

        // Construir value objects -- as excecoes de dominio chegam ao middleware como 400.
        var document = CpfCnpj.Create(request.Document);
        var email    = Email.Create(request.Email);
        var phone    = Phone.Create(request.Phone);
        var address  = Address.Create(
            request.ZipCode, request.Street, request.Number,
            request.District, request.City, request.State);

        Customer customer = request.Type == CustomerType.Individual
            ? Customer.CreateIndividual(
                request.Name, document, request.BirthOrFoundationDate, email, phone, address, clock)
            : Customer.CreateCompany(
                request.Name, document, request.BirthOrFoundationDate, email, phone, address,
                request.StateRegistration, request.IsStateRegistrationExempt);

        var metadata = new EventMetadata(
            CorrelationId: executionContext.CorrelationId,
            UserId:        executionContext.UserId);

        // Aggregate novo: expected version = 0.
        await eventStore.AppendAsync(
            customer.Id,
            "Customer",
            expectedVersion: 0,
            customer.DomainEvents,
            metadata,
            cancellationToken);

        await projectionWriter.ProjectAsync(customer.Id, customer.DomainEvents, cancellationToken);

        // Ambos os writes sao committed atomicamente pelo SaveChanges do DbContext.
        await readModelReader.SaveAsync(cancellationToken);

        LogCustomerCreated(logger, customer.Id, customer.Type);

        return customer.Id;
    }

    private static string NormalizeDigits(string raw) =>
        new string(raw.Where(char.IsDigit).ToArray());

    [LoggerMessage(Level = LogLevel.Information, Message = "Cliente criado: {CustomerId} ({Type})")]
    private static partial void LogCustomerCreated(
        ILogger logger, Guid customerId, CustomerType type);
}
