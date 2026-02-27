using CRM.Clients.Domain.Events;

namespace CRM.Clients.Infrastructure.Serialization;

// Registry central de tipos de evento. Registrar aqui ao adicionar novo IDomainEvent.
public sealed class EventTypeMapper
{
    private static readonly Dictionary<string, Type> NameToType =
        new()
        {
            [nameof(CustomerCreated)]        = typeof(CustomerCreated),
            [nameof(CustomerEmailChanged)]   = typeof(CustomerEmailChanged),
            [nameof(CustomerAddressUpdated)] = typeof(CustomerAddressUpdated),
            [nameof(CustomerPhoneChanged)]   = typeof(CustomerPhoneChanged),
            [nameof(CustomerTaxInfoUpdated)] = typeof(CustomerTaxInfoUpdated),
        };

    public static string GetEventType(IDomainEvent @event) =>
        @event.GetType().Name;

    public static Type GetClrType(string eventType) =>
        NameToType.TryGetValue(eventType, out Type? type)
            ? type
            : throw new InvalidOperationException(
                $"Tipo de evento desconhecido: '{eventType}'. Registrar em EventTypeMapper.");
}
