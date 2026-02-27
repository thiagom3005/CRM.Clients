namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Fornece dados do contexto de execucao atual (usuario e correlacao) para os handlers.
/// Em producao viriam do authn/authz; aqui e MVP via headers X-User e X-Correlation-Id.
/// </summary>
public interface IExecutionContextAccessor
{
    /// <summary>Identificador do usuario autenticado ou "anonymous".</summary>
    string UserId { get; }

    /// <summary>ID de correlacao da requisicao atual ou guid gerado se ausente.</summary>
    string CorrelationId { get; }
}
