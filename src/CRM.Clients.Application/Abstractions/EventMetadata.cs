namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Metadados de auditoria anexados a cada evento persistido.
/// MVP: campos opcionais; expandir conforme necessidade (tenant, trace etc).
/// </summary>
public sealed record EventMetadata(
    string? CorrelationId = null,
    string? CausationId = null,
    string? UserId = null)
{
    public static readonly EventMetadata Empty = new();
}
