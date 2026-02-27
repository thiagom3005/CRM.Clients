namespace CRM.Clients.Application.Common.Exceptions;

/// <summary>
/// Violacao de concorrencia otimista: outro processo modificou o aggregate antes deste.
/// O cliente deve retentar a operacao com o estado atual.
/// </summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(Guid aggregateId, int expectedVersion)
        : base(
            $"Conflito de concorrencia no aggregate {aggregateId}. " +
            $"Versao esperada: {expectedVersion}.")
    {
    }
}
