using System.Text.Json;

namespace CRM.Clients.Application.Customers.Queries.Models;

/// <summary>
/// Representa um evento do historico de um cliente para o endpoint GET /customers/{id}/events.
/// </summary>
public sealed record CustomerEventDto(
    Guid EventId,
    int Version,
    string EventType,
    JsonElement Data,
    string? UserId,
    string? CorrelationId,
    DateTimeOffset OccurredAtUtc);
