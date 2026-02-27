using CRM.Clients.Domain.Events;

namespace CRM.Clients.Infrastructure.Serialization;

/// <summary>
/// Registry central: string EventType &lt;-&gt; CLR Type.
/// Adicionar aqui cada novo tipo de evento do dominio.
/// Todos os membros sao estaticos pois o mapper nao possui estado de instancia.
/// </summary>
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
