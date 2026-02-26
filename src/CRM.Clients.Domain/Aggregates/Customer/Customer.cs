using CRM.Clients.Domain.Abstractions;
using CRM.Clients.Domain.Events;
using CRM.Clients.Domain.Exceptions;
using CRM.Clients.Domain.ValueObjects;

namespace CRM.Clients.Domain.Aggregates.Customer;

/// <summary>
/// Aggregate Root do modulo de clientes.
/// Concentra todas as invariantes de negocio relacionadas ao cliente.
///
/// TODO (Application): garantir unicidade de CPF/CNPJ e e-mail antes de criar o aggregate.
/// O dominio nao consulta repositorios -- essa checagem pertence a camada de aplicacao.
/// </summary>
public sealed class Customer
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }

    public CustomerType Type { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public CpfCnpj Document { get; private set; } = null!;

    /// <summary>Data de nascimento (PF) ou de fundacao (PJ).</summary>
    public DateOnly BirthOrFoundationDate { get; private set; }

    public Email Email { get; private set; } = null!;

    public Phone Phone { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    /// <summary>Inscricao Estadual -- obrigatoria para PJ nao isenta.</summary>
    public string? StateRegistration { get; private set; }

    /// <summary>
    /// Quando true, o cliente declarou isencao de IE e <see cref="StateRegistration"/> deve ser nulo.
    /// Compliance tributario: IE obrigatoria para PJ ativa no ICMS.
    /// </summary>
    public bool IsStateRegistrationExempt { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Numero de eventos ja persistidos no event store. Usado para concorrencia otimista.
    /// Incrementado pelo Rehydrate; permanece 0 em aggregates novos.
    /// </summary>
    public int Version { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Construtor privado -- criacao sempre via factory para aplicar invariantes.
    private Customer()
    {
    }

    // -------------------------------------------------------------------------
    // Factories
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cria um cliente Pessoa Fisica.
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
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        customer._domainEvents.Add(
            new CustomerCreated(
                customer.Id,
                customer.Type,
                customer.Name,
                customer.Document.Value,
                customer.BirthOrFoundationDate,
                customer.Email.Value,
                customer.Phone.Value,
                customer.Address.ZipCode,
                customer.Address.Street,
                customer.Address.Number,
                customer.Address.District,
                customer.Address.City,
                customer.Address.State,
                customer.StateRegistration,
                customer.IsStateRegistrationExempt,
                customer.CreatedAtUtc));

        return customer;
    }

    /// <summary>
    /// Cria um cliente Pessoa Juridica.
    /// Valida obrigatoriedade de IE ou isencao declarada.
    /// </summary>
    public static Customer CreateCompany(
        string name,
        CpfCnpj cnpj,
        DateOnly foundationDate,
        Email email,
        Phone phone,
        Address address,
        string? stateRegistration,
        bool isStateRegistrationExempt)
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
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        customer._domainEvents.Add(
            new CustomerCreated(
                customer.Id,
                customer.Type,
                customer.Name,
                customer.Document.Value,
                customer.BirthOrFoundationDate,
                customer.Email.Value,
                customer.Phone.Value,
                customer.Address.ZipCode,
                customer.Address.Street,
                customer.Address.Number,
                customer.Address.District,
                customer.Address.City,
                customer.Address.State,
                customer.StateRegistration,
                customer.IsStateRegistrationExempt,
                customer.CreatedAtUtc));

        return customer;
    }

    // -------------------------------------------------------------------------
    // Rehydration (event sourcing)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reconstroi o estado do aggregate a partir do historico de eventos persistidos.
    /// Nao gera novos domain events -- apenas aplica estado.
    /// </summary>
    public static Customer Rehydrate(IEnumerable<IDomainEvent> events)
    {
        var customer = new Customer();

        foreach (var e in events)
        {
            customer.Apply(e);
            customer.Version++;
        }

        return customer;
    }

    /// <summary>
    /// Aplica um evento para reconstituir estado. Nao adiciona ao DomainEvents.
    /// </summary>
    internal void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreated e:
                Id = e.CustomerId;
                Type = e.Type;
                Name = e.Name;
                Document = CpfCnpj.Create(e.Document);
                BirthOrFoundationDate = e.BirthOrFoundationDate;
                Email = Email.Create(e.Email);
                Phone = Phone.Create(e.Phone);
                Address = Address.Create(
                    e.ZipCode, e.Street, e.Number, e.District, e.City, e.State);
                StateRegistration = e.StateRegistration;
                IsStateRegistrationExempt = e.IsStateRegistrationExempt;
                CreatedAtUtc = e.OccurredAtUtc;
                UpdatedAtUtc = e.OccurredAtUtc;
                break;

            case CustomerEmailChanged e:
                Email = Email.Create(e.NewEmail);
                UpdatedAtUtc = e.OccurredAtUtc;
                break;

            case CustomerAddressUpdated e:
                Address = Address.Create(
                    e.ZipCode, e.Street, e.Number, e.District, e.City, e.State);
                UpdatedAtUtc = e.OccurredAtUtc;
                break;

            case CustomerPhoneChanged e:
                Phone = Phone.Create(e.NewPhone);
                UpdatedAtUtc = e.OccurredAtUtc;
                break;

            case CustomerTaxInfoUpdated e:
                StateRegistration = e.StateRegistration;
                IsStateRegistrationExempt = e.IsStateRegistrationExempt;
                UpdatedAtUtc = e.OccurredAtUtc;
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Metodos de mutacao
    // -------------------------------------------------------------------------

    /// <summary>Altera o e-mail do cliente e registra o evento correspondente.</summary>
    public void ChangeEmail(Email newEmail)
    {
        Email = newEmail;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            new CustomerEmailChanged(
                Id,
                newEmail.Value,
                DateTimeOffset.UtcNow));
    }

    /// <summary>Atualiza o endereco e registra o evento correspondente.</summary>
    public void UpdateAddress(Address newAddress)
    {
        Address = newAddress;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            new CustomerAddressUpdated(
                Id,
                newAddress.ZipCode,
                newAddress.Street,
                newAddress.Number,
                newAddress.District,
                newAddress.City,
                newAddress.State,
                DateTimeOffset.UtcNow));
    }

    /// <summary>Altera o telefone de contato e registra o evento correspondente.</summary>
    public void ChangePhone(Phone newPhone)
    {
        Phone = newPhone;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            new CustomerPhoneChanged(
                Id,
                newPhone.Value,
                DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Atualiza informacoes tributarias de PJ.
    /// Aplica as mesmas regras de validacao da criacao.
    /// </summary>
    public void UpdateTaxInfo(string? stateRegistration, bool isStateRegistrationExempt)
    {
        if (Type != CustomerType.Company)
        {
            throw new DomainException(
                "Informacoes tributarias de IE so se aplicam a Pessoa Juridica.");
        }

        ValidateStateRegistration(stateRegistration, isStateRegistrationExempt);

        StateRegistration = NormalizeStateRegistration(stateRegistration);
        IsStateRegistrationExempt = isStateRegistrationExempt;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(
            new CustomerTaxInfoUpdated(
                Id,
                StateRegistration,
                IsStateRegistrationExempt,
                DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Remove os eventos acumulados apos publicacao/persistencia.
    /// Chamado pela camada de Infrastructure ao salvar o aggregate.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // -------------------------------------------------------------------------
    // Validacoes internas
    // -------------------------------------------------------------------------

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Nome do cliente nao pode ser vazio.");
        }
    }

    /// <summary>
    /// Valida idade minima de 18 anos completos.
    /// Usa DateOnly para evitar ambiguidade de hora/fuso -- relogio injetado para testes deterministicos.
    /// </summary>
    private static void ValidateMinimumAge(DateOnly birthDate, IClock clock)
    {
        DateOnly today = clock.Today;
        int age = today.Year - birthDate.Year;

        // Corrige caso o aniversario ainda nao tenha ocorrido no ano corrente.
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        if (age < 18)
        {
            throw new DomainException(
                $"Pessoa Fisica deve ter no minimo 18 anos. Idade calculada: {age} ano(s).");
        }
    }

    /// <summary>
    /// Compliance tributario: PJ deve ter IE preenchida OU declarar isencao.
    /// IE e isencao sao mutuamente exclusivos.
    /// </summary>
    private static void ValidateStateRegistration(string? stateRegistration, bool isExempt)
    {
        bool hasIE = !string.IsNullOrWhiteSpace(stateRegistration);

        if (isExempt && hasIE)
        {
            throw new DomainException(
                "Cliente isento de IE nao pode informar Inscricao Estadual.");
        }

        if (!isExempt && !hasIE)
        {
            throw new DomainException(
                "Pessoa Juridica nao isenta deve informar a Inscricao Estadual.");
        }
    }

    private static string? NormalizeStateRegistration(string? stateRegistration)
    {
        return string.IsNullOrWhiteSpace(stateRegistration)
            ? null
            : stateRegistration.Trim();
    }
}
