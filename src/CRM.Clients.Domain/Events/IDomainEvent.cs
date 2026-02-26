namespace CRM.Clients.Domain.Events;

/// <summary>
/// Marca um fato que ocorreu no dominio e pode ser observado por outras partes do sistema.
/// </summary>
public interface IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; }
}
