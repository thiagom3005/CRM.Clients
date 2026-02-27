using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Customers.Queries.Models;
using CRM.Clients.Domain.Aggregates.Customer;
using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de leitura usando EF Core + AsNoTracking.
/// Projeta diretamente para DTO no banco -- nao materializa a entidade inteira.
/// Usa ILike (Postgres) para busca case-insensitive sem funcao no lado do cliente.
/// </summary>
public sealed class EfCustomerReadRepository(AppDbContext dbContext) : ICustomerReadRepository
{
    public Task<CustomerDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.CustomerReadModels
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDetailsDto(
                c.Id,
                (CustomerType)c.Type,
                c.Name,
                c.Document,
                c.BirthOrFoundationDate,
                c.Email,
                c.Phone,
                c.ZipCode,
                c.Street,
                c.Number,
                c.District,
                c.City,
                c.State,
                c.StateRegistration,
                c.IsStateRegistrationExempt,
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<CustomerListItemDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string? sort,
        CancellationToken ct = default)
    {
        var baseQuery = dbContext.CustomerReadModels.AsNoTracking();

        // Filtro por nome, e-mail (ILike = case-insensitive no Postgres) ou documento exato.
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            string likePattern = $"%{term}%";
            string digitsOnly = new string(term.Where(char.IsDigit).ToArray());

            baseQuery = baseQuery.Where(c =>
                EF.Functions.ILike(c.Name, likePattern) ||
                EF.Functions.ILike(c.Email, likePattern) ||
                (digitsOnly.Length > 0 && c.Document == digitsOnly));
        }

        // Conta antes de paginar para preencher Total sem carregar linhas.
        int total = await baseQuery.CountAsync(ct);

        // Ordenacao configuravel; default e mais recente primeiro.
        var ordered = sort switch
        {
            "nameAsc"       => baseQuery.OrderBy(c => c.Name),
            "nameDesc"      => baseQuery.OrderByDescending(c => c.Name),
            "updatedAtAsc"  => baseQuery.OrderBy(c => c.UpdatedAtUtc),
            _               => baseQuery.OrderByDescending(c => c.UpdatedAtUtc)
        };

        // Projeta direto para DTO -- evita carregar colunas nao usadas na lista.
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(
                c.Id,
                (CustomerType)c.Type,
                c.Name,
                c.Document,
                c.Email,
                c.City,
                c.State,
                c.UpdatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>(items, page, pageSize, total);
    }
}
