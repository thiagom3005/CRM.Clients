using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Customers.Queries.Models;
using CRM.Clients.Domain.Aggregates.Customer;
using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio somente-leitura otimizado para consultas do read side.
/// Usa AsNoTracking e projecao direta para DTO -- o write model nao deve ser
/// usado para leitura para nao pressionar o change tracker desnecessariamente.
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
        // Protege o banco contra paginacao invalida ou abusiva.
        page     = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var baseQuery = dbContext.CustomerReadModels.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();

            if (term.All(char.IsDigit))
            {
                // Evita full scan quando o usuario busca por CPF/CNPJ.
                baseQuery = baseQuery.Where(c => c.Document == term);
            }
            else
            {
                // Busca textual: ILike e case-insensitive no Postgres sem funcao client-side.
                string likePattern = $"%{term}%";
                baseQuery = baseQuery.Where(c =>
                    EF.Functions.ILike(c.Name, likePattern) ||
                    EF.Functions.ILike(c.Email, likePattern));
            }
        }

        // MVP utiliza COUNT para paginacao exata.
        // Em alto volume considerar estrategia cursor-based (seek method).
        int total = await baseQuery.CountAsync(ct);

        // Qualquer valor desconhecido em sort cai no default -- sem excecao nem resultado vazio.
        var ordered = sort switch
        {
            "nameAsc"      => baseQuery.OrderBy(c => c.Name),
            "nameDesc"     => baseQuery.OrderByDescending(c => c.Name),
            "updatedAtAsc" => baseQuery.OrderBy(c => c.UpdatedAtUtc),
            _              => baseQuery.OrderByDescending(c => c.UpdatedAtUtc)
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
