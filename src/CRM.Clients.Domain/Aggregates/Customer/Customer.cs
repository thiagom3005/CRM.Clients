using CRM.Clients.Domain.Abstractions;
using CRM.Clients.Domain.Events;
using CRM.Clients.Domain.Exceptions;
using CRM.Clients.Domain.ValueObjects;

namespace CRM.Clients.Domain.Aggregates.Customer;

// Unicidade de CPF/CNPJ e e-mail é responsabilidade do handler — dominio nao consulta repos.
public sealed class Customer
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }

    public CustomerType Type { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public CpfCnpj Document { get; private set; } = null!;

    // Campo unificado para PF (nascimento) e PJ (fundacao).
    public DateOnly BirthOrFoundationDate { get; private set; }

    public Email Email { get; private set; } = null!;

    public Phone Phone { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    // IE obrigatoria para PJ ativa no ICMS, salvo declaracao de isencao.
    public string? StateRegistration { get; private set; }

    public bool IsStateRegistrationExempt { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    // Numero de eventos persistidos — controle de concorrencia otimista no AppendAsync.
    public int Version { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Customer() { }

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

        customer._domainEvents.Add(new CustomerCreated(
            customer.Id, customer.Type, customer.Name, customer.Document.Value,
            customer.BirthOrFoundationDate, customer.Email.Value, customer.Phone.Value,
            customer.Address.ZipCode, customer.Address.Street, customer.Address.Number,
            customer.Address.District, customer.Address.City, customer.Address.State,
            customer.StateRegistration, customer.IsStateRegistrationExempt,
            customer.CreatedAtUtc));

        return customer;
    }

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

        customer._domainEvents.Add(new CustomerCreated(
            customer.Id, customer.Type, customer.Name, customer.Document.Value,
            customer.BirthOrFoundationDate, customer.Email.Value, customer.Phone.Value,
            customer.Address.ZipCode, customer.Address.Street, customer.Address.Number,
            customer.Address.District, customer.Address.City, customer.Address.State,
            customer.StateRegistration, customer.IsStateRegistrationExempt,
            customer.CreatedAtUtc));

        return customer;
    }

    // Reconstroi estado a partir do historico persistido. Nao emite novos eventos.
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
                Address = Address.Create(e.ZipCode, e.Street, e.Number, e.District, e.City, e.State);
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
                Address = Address.Create(e.ZipCode, e.Street, e.Number, e.District, e.City, e.State);
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

    public void ChangeEmail(Email newEmail)
    {
        Email = newEmail;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        _domainEvents.Add(new CustomerEmailChanged(Id, newEmail.Value, DateTimeOffset.UtcNow));
    }

    public void UpdateAddress(Address newAddress)
    {
        Address = newAddress;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        _domainEvents.Add(new CustomerAddressUpdated(
            Id,
            newAddress.ZipCode, newAddress.Street, newAddress.Number,
            newAddress.District, newAddress.City, newAddress.State,
            DateTimeOffset.UtcNow));
    }

    public void ChangePhone(Phone newPhone)
    {
        Phone = newPhone;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        _domainEvents.Add(new CustomerPhoneChanged(Id, newPhone.Value, DateTimeOffset.UtcNow));
    }

    // IE e isencao sao mutuamente exclusivos — mesmas regras da criacao.
    public void UpdateTaxInfo(string? stateRegistration, bool isStateRegistrationExempt)
    {
        if (Type != CustomerType.Company)
            throw new DomainException("Informacoes tributarias de IE so se aplicam a Pessoa Juridica.");

        ValidateStateRegistration(stateRegistration, isStateRegistrationExempt);

        StateRegistration = NormalizeStateRegistration(stateRegistration);
        IsStateRegistrationExempt = isStateRegistrationExempt;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        _domainEvents.Add(new CustomerTaxInfoUpdated(
            Id, StateRegistration, IsStateRegistrationExempt, DateTimeOffset.UtcNow));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do cliente nao pode ser vazio.");
    }

    // DateOnly evita ambiguidade de fuso; relogio injetado torna testes deterministicos.
    private static void ValidateMinimumAge(DateOnly birthDate, IClock clock)
    {
        DateOnly today = clock.Today;
        int age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
            age--;

        if (age < 18)
            throw new DomainException(
                $"Pessoa Fisica deve ter no minimo 18 anos. Idade calculada: {age} ano(s).");
    }

    // PJ deve ter IE OU declarar isencao — nunca os dois, nunca nenhum.
    private static void ValidateStateRegistration(string? stateRegistration, bool isExempt)
    {
        bool hasIE = !string.IsNullOrWhiteSpace(stateRegistration);

        if (isExempt && hasIE)
            throw new DomainException("Cliente isento de IE nao pode informar Inscricao Estadual.");

        if (!isExempt && !hasIE)
            throw new DomainException("Pessoa Juridica nao isenta deve informar a Inscricao Estadual.");
    }

    private static string? NormalizeStateRegistration(string? stateRegistration) =>
        string.IsNullOrWhiteSpace(stateRegistration) ? null : stateRegistration.Trim();
}
