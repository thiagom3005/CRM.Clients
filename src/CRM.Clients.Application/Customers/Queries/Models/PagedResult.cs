namespace CRM.Clients.Application.Customers.Queries.Models;

/// <summary>Envelope paginado generico para listas de consulta.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total)
{
    /// <summary>Total de paginas calculado a partir de Total e PageSize.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}
