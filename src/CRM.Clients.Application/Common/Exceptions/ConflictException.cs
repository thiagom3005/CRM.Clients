namespace CRM.Clients.Application.Common.Exceptions;

/// <summary>
/// Conflito de unicidade: um recurso com os dados informados ja existe.
/// Mapeado para 409 Conflict no middleware da API.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
