namespace CRM.Clients.Application.Common.Exceptions;

/// <summary>
/// Lancada quando um servico externo nao esta disponivel apos esgotar as tentativas de retry.
/// Mapeada para HTTP 503 pelo GlobalExceptionMiddleware.
/// </summary>
public sealed class ServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
