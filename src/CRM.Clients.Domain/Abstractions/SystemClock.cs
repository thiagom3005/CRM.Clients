namespace CRM.Clients.Domain.Abstractions;

/// <summary>
/// Implementação de produção de <see cref="IClock"/>. Retorna <c>DateTime.UtcNow</c>.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
