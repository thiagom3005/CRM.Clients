using CRM.Clients.Domain.Abstractions;

namespace CRM.Clients.Domain.Tests.Fakes;

/// <summary>
/// Relogio deterministico para uso exclusivo em testes.
/// Permite fixar "hoje" e evitar flakiness dependente de data real.
/// </summary>
public sealed class FakeClock(DateOnly today) : IClock
{
    public DateOnly Today => today;
}
