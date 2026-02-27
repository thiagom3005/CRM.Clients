using System.Text.Json;
using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Customers.Queries.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacao de IEventHistoryReader usando EF Core / Postgres.
/// Projecao direta para CustomerEventDto sem carregar entidades completas.
/// </summary>
public sealed class EfEventHistoryReader(AppDbContext dbContext) : IEventHistoryReader
{
    public async Task<PagedResult<CustomerEventDto>> GetPagedAsync(
        Guid aggregateId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        int safePage     = Math.Max(1, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Entities.EventRecord> query = dbContext.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .AsNoTracking();

        int total = await query.CountAsync(ct);

        if (total == 0)
        {
            return new PagedResult<CustomerEventDto>(
                Items:    [],
                Page:     safePage,
                PageSize: safePageSize,
                Total:    0);
        }

        List<Entities.EventRecord> records = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(ct);

        List<CustomerEventDto> items = records.ConvertAll(r =>
        {
            // Parse do JSON de metadata para extrair UserId e CorrelationId.
            string? userId        = null;
            string? correlationId = null;

            if (!string.IsNullOrEmpty(r.Metadata))
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(r.Metadata);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("UserId", out JsonElement uid))
                        userId = uid.GetString();

                    if (root.TryGetProperty("CorrelationId", out JsonElement cid))
                        correlationId = cid.GetString();
                }
                catch (JsonException)
                {
                    // Metadados malformados nao devem impedir a leitura dos eventos.
                }
            }

            // Clone do JsonElement para evitar ObjectDisposedException apos o Dispose do JsonDocument.
            JsonElement dataElement;
            using (JsonDocument dataDoc = JsonDocument.Parse(r.Data))
            {
                dataElement = dataDoc.RootElement.Clone();
            }

            return new CustomerEventDto(
                EventId:       r.Id,
                Version:       r.Version,
                EventType:     r.EventType,
                Data:          dataElement,
                UserId:        userId,
                CorrelationId: correlationId,
                OccurredAtUtc: r.OccurredAtUtc);
        });

        return new PagedResult<CustomerEventDto>(
            Items:    items,
            Page:     safePage,
            PageSize: safePageSize,
            Total:    total);
    }
}
