using Serilog.Context;

namespace CRM.Clients.Api.Middleware;

/// <summary>
/// Middleware que garante que toda requisicao tenha um CorrelationId e um UserId.
/// Le X-Correlation-Id e X-User dos headers de entrada; gera um GUID se ausente.
/// Armazena os valores em HttpContext.Items para que IExecutionContextAccessor os leia,
/// e enriquece o LogContext do Serilog para rastreabilidade em logs estruturados.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string UserHeader        = "X-User";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
                               ?? Guid.NewGuid().ToString();

        string userId = context.Request.Headers[UserHeader].FirstOrDefault()
                        ?? "anonymous";

        // Persiste para IExecutionContextAccessor.
        context.Items[CorrelationHeader] = correlationId;
        context.Items[UserHeader]        = userId;

        // Adiciona ao header de resposta para facilitar debug.
        context.Response.Headers[CorrelationHeader] = correlationId;

        // Enriquece todos os logs emitidos durante o processamento da requisicao.
        using IDisposable _ = LogContext.PushProperty("CorrelationId", correlationId);
        using IDisposable __ = LogContext.PushProperty("UserId", userId);

        await next(context);
    }
}
