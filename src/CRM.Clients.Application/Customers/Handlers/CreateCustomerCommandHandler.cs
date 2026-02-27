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
public sealed class CreateCustomerCommandHandler(
    IEventStore eventStore,
    ICustomerProjectionWriter projectionWriter,
    ICustomerReadModelReader readModelReader,
    IClock clock,
    ILogger<CreateCustomerCommandHandler> logger)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        // Normalizar antes de checar unicidade (o dominio faz o mesmo internamente).
        string normalizedDoc = NormalizeDigits(cmd.Document);
        string normalizedEmail = cmd.Email.Trim().ToLowerInvariant();

        if (await readModelReader.DocumentExistsAsync(normalizedDoc, ct))
        {
            throw new ConflictException("CPF/CNPJ ja cadastrado.");
        }

        if (await readModelReader.EmailExistsAsync(normalizedEmail, ct))
        {
            throw new ConflictException("E-mail ja cadastrado.");
        }

        // Construir value objects -- as excecoes de dominio chegam ao middleware como 400.
        var document = CpfCnpj.Create(cmd.Document);
        var email = Email.Create(cmd.Email);
        var phone = Phone.Create(cmd.Phone);
        var address = Address.Create(
            cmd.ZipCode, cmd.Street, cmd.Number, cmd.District, cmd.City, cmd.State);

        Customer customer = cmd.Type == CustomerType.Individual
            ? Customer.CreateIndividual(
                cmd.Name, document, cmd.BirthOrFoundationDate, email, phone, address, clock)
            : Customer.CreateCompany(
                cmd.Name, document, cmd.BirthOrFoundationDate, email, phone, address,
                cmd.StateRegistration, cmd.IsStateRegistrationExempt);

        // Aggregate novo: expected version = 0.
        await eventStore.AppendAsync(
            customer.Id,
            "Customer",
            expectedVersion: 0,
            customer.DomainEvents,
            EventMetadata.Empty,
            ct);

        await projectionWriter.ProjectAsync(customer.Id, customer.DomainEvents, ct);

        // Ambos os writes sao committed atomicamente pelo SaveChanges do DbContext.
        await readModelReader.SaveAsync(ct);

        logger.LogInformation(
            "Cliente criado: {CustomerId} ({Type})", customer.Id, customer.Type);

        return customer.Id;
    }

    private static string NormalizeDigits(string raw) =>
        new string(raw.Where(char.IsDigit).ToArray());
}
