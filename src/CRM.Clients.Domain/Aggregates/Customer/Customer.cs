using CRM.Clients.Domain.Abstractions;
using CRM.Clients.Domain.Events;
using CRM.Clients.Domain.Exceptions;
using CRM.Clients.Domain.ValueObjects;

namespace CRM.Clients.Domain.Aggregates.Customer;

/// <summary>
/// Aggregate Root do módulo de clientes.
/// Concentra todas as invariantes de negócio relacionadas ao cliente.
///
/// TODO (Application): garantir unicidade de CPF/CNPJ e e-mail antes de criar o aggregate.
/// O domínio não consulta repositórios — essa checagem pertence à camada de aplicação.
/// </summary>
public sealed class Customer
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public CustomerType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CpfCnpj Document { get; private set; } = null!;

    /// <summary>Data de nascimento (PF) ou de fundação (PJ).</summary>
    public DateOnly BirthOrFoundationDate { get; private set; }

    public Email Email { get; private set; } = null!;
    public Phone Phone { get; private set; } = null!;
    public Address Address { get; private set; } = null!;

    /// <summary>Inscrição Estadual — obrigatória para PJ não isenta.</summary>
    public string? StateRegistration { get; private set; }

    /// <summary>
    /// Quando true, o cliente declarou isenção de IE e <see cref="StateRegistration"/> deve ser nulo.
    /// Compliance tributário: IE obrigatória para PJ ativa no ICMS.
    /// </summary>
    public bool IsStateRegistrationExempt { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Construtor privado — criação sempre via factory para aplicar invariantes.
    private Customer() { }

    // -------------------------------------------------------------------------
    // Factories
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cria um cliente Pessoa Física.
    /// Valida que o cliente possui ao menos 18 anos completos na data atual UTC.
    /// </summary>
    public static Customer CreateIndividual(
        string name,
        CpfCnpj cpf,
        DateOnly birthDate,
        Email email,
        Phone phone,
        Address address,
        IClock clock)
    {
        ValidateName(name);
        ValidateMinimumAge(birthDate, clock);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Type = CustomerType.Individual,
            Name = name.Trim(),
            Document = cpf,
            BirthOrFoundationDate = birthDate,
            Email = email,
            Phone = phone,
            Address = address,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        customer._domainEvents.Add(new CustomerCreated(
            customer.Id,
            customer.Type,
            customer.Document.Value,
            customer.Email.Value,
            customer.CreatedAtUtc));

        return customer;
    }

    /// <summary>
    /// Cria um cliente Pessoa Jurídica.
    /// Valida obrigatoriedade de IE ou isenção declarada.
    /// </summary>
    public static Customer CreateCompany(
        string name,
        CpfCnpj cnpj,
        DateOnly foundationDate,
        Email email,
        Phone phone,
        Address address,
        string? stateRegistration,
        bool isStateRegistrationExempt,
        IClock clock)
    {
        ValidateName(name);
        ValidateStateRegistration(stateRegistration, isStateRegistrationExempt);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Type = CustomerType.Company,
            Name = name.Trim(),
            Document = cnpj,
            BirthOrFoundationDate = foundationDate,
            Email = email,
            Phone = phone,
            Address = address,
            StateRegistration = NormalizeStateRegistration(stateRegistration),
            IsStateRegistrationExempt = isStateRegistrationExempt,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        customer._domainEvents.Add(new CustomerCreated(
            customer.Id,
            customer.Type,
            customer.Document.Value,
            customer.Email.Value,
            customer.CreatedAtUtc));

        return customer;
    }

    // -------------------------------------------------------------------------
    // Métodos de mutação
    // -------------------------------------------------------------------------

    /// <summary>Altera o e-mail do cliente e registra o evento correspondente.</summary>
    public void ChangeEmail(Email newEmail, IClock clock)
    {
        Email = newEmail;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(new CustomerEmailChanged(Id, newEmail.Value, DateTimeOffset.UtcNow));
    }

    /// <summary>Atualiza o endereço e registra o evento correspondente.</summary>
    public void UpdateAddress(Address newAddress, IClock clock)
    {
        Address = newAddress;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(new CustomerAddressUpdated(Id, newAddress.ZipCode, newAddress.City, newAddress.State, DateTimeOffset.UtcNow));
    }

    /// <summary>Altera o telefone de contato e registra o evento correspondente.</summary>
    public void ChangePhone(Phone newPhone, IClock clock)
    {
        Phone = newPhone;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(new CustomerPhoneChanged(Id, newPhone.Value, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Atualiza informações tributárias de PJ.
    /// Aplica as mesmas regras de validação da criação.
    /// </summary>
    public void UpdateTaxInfo(string? stateRegistration, bool isStateRegistrationExempt, IClock clock)
    {
        if (Type != CustomerType.Company)
        {
            throw new DomainException("Informações tributárias de IE só se aplicam a Pessoa Jurídica.");
        }

        ValidateStateRegistration(stateRegistration, isStateRegistrationExempt);

        StateRegistration = NormalizeStateRegistration(stateRegistration);
        IsStateRegistrationExempt = isStateRegistrationExempt;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(new CustomerTaxInfoUpdated(Id, StateRegistration, IsStateRegistrationExempt, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Remove os eventos acumulados após publicação/persistência.
    /// Chamado pela camada de Infrastructure ao salvar o aggregate.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // -------------------------------------------------------------------------
    // Validações internas
    // -------------------------------------------------------------------------

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Nome do cliente não pode ser vazio.");
        }
    }

    /// <summary>
    /// Valida idade mínima de 18 anos completos.
    /// Usa DateOnly para evitar ambiguidade de hora/fuso — relógio injetado para testes determinísticos.
    /// </summary>
    private static void ValidateMinimumAge(DateOnly birthDate, IClock clock)
    {
        DateOnly today = clock.Today;
        int age = today.Year - birthDate.Year;

        // Corrige caso o aniversário ainda não tenha ocorrido no ano corrente.
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        if (age < 18)
        {
            throw new DomainException($"Pessoa Física deve ter no mínimo 18 anos. Idade calculada: {age} ano(s).");
        }
    }

    /// <summary>
    /// Compliance tributário: PJ deve ter IE preenchida OU declarar isenção.
    /// IE e isenção são mutuamente exclusivos.
    /// </summary>
    private static void ValidateStateRegistration(string? stateRegistration, bool isExempt)
    {
        bool hasIE = !string.IsNullOrWhiteSpace(stateRegistration);

        if (isExempt && hasIE)
        {
            throw new DomainException("Cliente isento de IE não pode informar Inscrição Estadual.");
        }

        if (!isExempt && !hasIE)
        {
            throw new DomainException("Pessoa Jurídica não isenta deve informar a Inscrição Estadual.");
        }
    }

    private static string? NormalizeStateRegistration(string? stateRegistration)
        => string.IsNullOrWhiteSpace(stateRegistration) ? null : stateRegistration.Trim();
}
