namespace CRM.Clients.Infrastructure.Persistence.Entities;

/// <summary>
/// Linha na tabela "events". Cada linha representa um domain event persistido.
/// </summary>
public sealed class EventRecord
{
    public Guid Id { get; set; }

    public Guid AggregateId { get; set; }

    /// <summary>Nome do tipo do aggregate (ex.: "Customer").</summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>Versao sequencial dentro do aggregate (1-based). Garante ordenacao.</summary>
    public int Version { get; set; }

    /// <summary>Nome do tipo CLR do evento (ex.: "CustomerCreated").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Payload JSON do evento.</summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>Metadados de auditoria (correlationId, userId etc).</summary>
    public string Metadata { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }
}
