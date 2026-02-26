using CRM.Clients.Application.Abstractions;
using CRM.Clients.Domain.Events;
using CRM.Clients.Infrastructure.Persistence.Entities;

namespace CRM.Clients.Infrastructure.Persistence.Repositories;

/// <summary>
/// Aplica eventos no read model customer_read_model.
/// Nao chama SaveChanges -- a transacao e gerenciada pelo handler.
/// </summary>
public sealed class CustomerProjectionWriter(AppDbContext dbContext) : ICustomerProjectionWriter
{
    public async Task ProjectAsync(
        Guid aggregateId,
        IReadOnlyCollection<IDomainEvent> newEvents,
        CancellationToken ct = default)
    {
        // Busca o read model existente (nulo se for o primeiro evento do aggregate).
        var model = await dbContext.CustomerReadModels.FindAsync([aggregateId], ct);

        foreach (var @event in newEvents)
        {
            switch (@event)
            {
                case CustomerCreated e:
                    model = MapFromCreated(e);
                    dbContext.CustomerReadModels.Add(model);
                    break;

                case CustomerEmailChanged e when model is not null:
                    model.Email = e.NewEmail;
                    model.UpdatedAtUtc = e.OccurredAtUtc;
                    break;

                case CustomerAddressUpdated e when model is not null:
                    model.ZipCode = e.ZipCode;
                    model.Street = e.Street;
                    model.Number = e.Number;
                    model.District = e.District;
                    model.City = e.City;
                    model.State = e.State;
                    model.UpdatedAtUtc = e.OccurredAtUtc;
                    break;

                case CustomerPhoneChanged e when model is not null:
                    model.Phone = e.NewPhone;
                    model.UpdatedAtUtc = e.OccurredAtUtc;
                    break;

                case CustomerTaxInfoUpdated e when model is not null:
                    model.StateRegistration = e.StateRegistration;
                    model.IsStateRegistrationExempt = e.IsStateRegistrationExempt;
                    model.UpdatedAtUtc = e.OccurredAtUtc;
                    break;
            }
        }
    }

    private static CustomerReadModel MapFromCreated(CustomerCreated e) =>
        new()
        {
            Id = e.CustomerId,
            Type = (int)e.Type,
            Name = e.Name,
            Document = e.Document,
            BirthOrFoundationDate = e.BirthOrFoundationDate,
            Email = e.Email,
            Phone = e.Phone,
            ZipCode = e.ZipCode,
            Street = e.Street,
            Number = e.Number,
            District = e.District,
            City = e.City,
            State = e.State,
            StateRegistration = e.StateRegistration,
            IsStateRegistrationExempt = e.IsStateRegistrationExempt,
            CreatedAtUtc = e.OccurredAtUtc,
            UpdatedAtUtc = e.OccurredAtUtc,
        };
}
