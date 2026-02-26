namespace CRM.Clients.Domain.Abstractions;

/// <summary>
/// Abstração de relógio para tornar validações de data testáveis de forma determinística.
/// Injete <see cref="SystemClock"/> em produção e <c>FakeClock</c> nos testes.
/// </summary>
public interface IClock
{
    /// <summary>Data atual em UTC (sem hora), usada para calcular idades e prazos.</summary>
    public DateOnly Today { get; }
}
