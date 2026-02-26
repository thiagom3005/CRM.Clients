using CRM.Clients.Domain.Events;

namespace CRM.Clients.Infrastructure.Serialization;

/// <summary>
/// Registry central: string EventType <-> CLR Type.
/// Adicionar aqui cada novo tipo de evento do dominio.
/// </summary>
public sealed class EventTypeMapper
{
    private static readonly IReadOnlyDictionary<string, Type> NameToType =
        new Dictionary<string, Type>
        {
            [nameof(CustomerCreated)] = typeof(CustomerCreated),
            [nameof(CustomerEmailChanged)] = typeof(CustomerEmailChanged),
            [nameof(CustomerAddressUpdated)] = typeof(CustomerAddressUpdated),
            [nameof(CustomerPhoneChanged)] = typeof(CustomerPhoneChanged),
            [nameof(CustomerTaxInfoUpdated)] = typeof(CustomerTaxInfoUpdated),
        };

    public string GetEventType(IDomainEvent @event) =>
        @event.GetType().Name;

    public Type GetClrType(string eventType) =>
        NameToType.TryGetValue(eventType, out var type)
            ? type
            : throw new InvalidOperationException(
                $"Tipo de evento desconhecido: '{eventType}'. Registrar em EventTypeMapper.");
}
