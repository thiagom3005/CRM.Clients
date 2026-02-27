using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consultas de unicidade no read model e commit atomico de eventos + projection.
/// Compartilha o mesmo DbContext do PgEventStore e CustomerProjectionWriter (escopo de request).
/// </summary>
public sealed class CustomerReadModelReader(AppDbContext dbContext) : ICustomerReadModelReader
{
    public Task<bool> DocumentExistsAsync(string normalizedDocument, CancellationToken ct = default) =>
        dbContext.CustomerReadModels
            .AsNoTracking()
            .AnyAsync(c => c.Document == normalizedDocument, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default) =>
        dbContext.CustomerReadModels
            .AsNoTracking()
            .AnyAsync(c => c.Email == normalizedEmail, ct);

    /// <summary>
    /// Commita eventos + projection atomicamente.
    /// Converte DbUpdateException de unique_violation em ConcurrencyException.
    /// </summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("23505") == true
               || ex.InnerException?.Message.Contains("unique") == true)
        {
            // Violacao de UNIQUE(AggregateId, Version) indica corrida concorrente.
            // Violacoes de UNIQUE(Document) e UNIQUE(Email) chegam como ConflictException.
            throw new ConcurrencyException(Guid.Empty, 0);
        }
    }
}
