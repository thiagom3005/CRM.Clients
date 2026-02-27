using Serilog.Context;

namespace CRM.Clients.Api.Middleware;

// Deve rodar antes do GlobalExceptionMiddleware para que erros ja incluam o CorrelationId.
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

        context.Items[CorrelationHeader] = correlationId;
        context.Items[UserHeader]        = userId;

        // Echo de volta para facilitar rastreio no cliente.
        context.Response.Headers[CorrelationHeader] = correlationId;

        using IDisposable _ = LogContext.PushProperty("CorrelationId", correlationId);
        using IDisposable __ = LogContext.PushProperty("UserId", userId);

        await next(context);
    }
}
