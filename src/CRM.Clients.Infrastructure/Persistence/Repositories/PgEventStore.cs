using System.Text.Json;
using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Domain.Events;
using CRM.Clients.Infrastructure.Persistence.Entities;
using CRM.Clients.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacao do event store usando Postgres + EF Core.
/// AppendAsync apenas registra as entidades no DbContext -- SaveChanges e
/// responsabilidade do handler (permite atomicidade com a projection).
/// </summary>
public sealed class PgEventStore(
    AppDbContext dbContext,
    EventTypeMapper typeMapper) : IEventStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = false };

    public async Task<IReadOnlyList<IDomainEvent>> LoadAsync(
        Guid aggregateId,
        CancellationToken ct = default)
    {
        var records = await dbContext.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .AsNoTracking()
            .ToListAsync(ct);

        return records
            .Select(Deserialize)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Registra os eventos no contexto. NAO chama SaveChanges.
    /// A violacao do indice UNIQUE (AggregateId, Version) sera detectada
    /// no SaveChanges do handler e convertida em ConcurrencyException.
    /// </summary>
    public Task AppendAsync(
        Guid aggregateId,
        string aggregateType,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        EventMetadata metadata,
        CancellationToken ct = default)
    {
        int version = expectedVersion;

        foreach (var @event in events)
        {
            version++;

            dbContext.Events.Add(new EventRecord
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                Version = version,
                EventType = typeMapper.GetEventType(@event),
                Data = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions),
                Metadata = JsonSerializer.Serialize(metadata, JsonOptions),
                OccurredAtUtc = @event.OccurredAtUtc,
            });
        }

        return Task.CompletedTask;
    }

    private IDomainEvent Deserialize(EventRecord record)
    {
        var clrType = typeMapper.GetClrType(record.EventType);
        return (IDomainEvent)JsonSerializer.Deserialize(record.Data, clrType, JsonOptions)!;
    }
}
