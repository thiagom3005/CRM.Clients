namespace CRM.Clients.Domain.Abstractions;

/// <summary>
/// Abstracao de relogio para tornar validacoes de data testaveis de forma deterministica.
/// Injete <see cref="SystemClock"/> em producao e <c>FakeClock</c> nos testes.
/// </summary>
public interface IClock
{
    /// <summary>Data atual em UTC (sem hora), usada para calcular idades e prazos.</summary>
    public DateOnly Today { get; }
}
