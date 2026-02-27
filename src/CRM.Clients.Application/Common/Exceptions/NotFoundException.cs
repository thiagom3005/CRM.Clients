namespace CRM.Clients.Application.Common.Exceptions;

/// <summary>
/// Recurso nao encontrado. Mapeado para 404 NotFound no middleware da API.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
