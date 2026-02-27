using CRM.Clients.Application.Customers.Queries.Models;

namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Repositorio somente-leitura do read model.
/// Separado do ICustomerReadModelReader (write side) para deixar clara a
/// fronteira entre projecao atomica (write) e consulta (read).
/// </summary>
public interface ICustomerReadRepository
{
    Task<CustomerDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Busca paginada com filtro opcional por nome, e-mail ou documento.
    /// sort aceita: "nameAsc", "nameDesc", "updatedAtAsc", "updatedAtDesc" (default).
    /// </summary>
    Task<PagedResult<CustomerListItemDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string? sort,
        CancellationToken ct = default);
}
