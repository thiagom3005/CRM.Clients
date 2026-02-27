using CRM.Clients.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CRM.Clients.Infrastructure.Http;

/// <summary>
/// Implementacao de IExecutionContextAccessor que le os headers
/// X-User e X-Correlation-Id populados pelo CorrelationIdMiddleware.
/// Usa IHttpContextAccessor para acessar o contexto da requisicao corrente.
/// </summary>
public sealed class HttpExecutionContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IExecutionContextAccessor
{
    public string UserId =>
        httpContextAccessor.HttpContext?.Items["X-User"] as string ?? "anonymous";

    public string CorrelationId =>
        httpContextAccessor.HttpContext?.Items["X-Correlation-Id"] as string
        ?? Guid.NewGuid().ToString();
}
