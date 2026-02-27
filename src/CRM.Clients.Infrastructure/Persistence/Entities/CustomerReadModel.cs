namespace CRM.Clients.Infrastructure.Persistence.Entities;

/// <summary>
/// Projecao denormalizada do aggregate Customer.
/// Atualizada sincronamente na mesma transacao do append de eventos (MVP).
/// Indices UNIQUE em Document e Email garantem unicidade no banco.
/// </summary>
public sealed class CustomerReadModel
{
    public Guid Id { get; set; }

    /// <summary>Valor do enum CustomerType (1=Individual, 2=Company).</summary>
    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public DateOnly BirthOrFoundationDate { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? StateRegistration { get; set; }

    public bool IsStateRegistrationExempt { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
