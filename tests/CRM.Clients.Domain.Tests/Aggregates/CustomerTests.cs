using CRM.Clients.Domain.Aggregates.Customer;
using CRM.Clients.Domain.Events;
using CRM.Clients.Domain.Exceptions;
using CRM.Clients.Domain.Tests.Fakes;
using CRM.Clients.Domain.ValueObjects;
using Xunit;

namespace CRM.Clients.Domain.Tests.Aggregates;

public sealed class CustomerTests
{
    // Data fixa para todos os testes — "hoje" é 2025-06-15.
    private static readonly FakeClock Clock = new(new DateOnly(2025, 6, 15));

    // Helpers para criar VOs válidos sem repetição nos testes.
    private static CpfCnpj ValidCpf() => CpfCnpj.Create("529.982.247-25");
    private static CpfCnpj ValidCnpj() => CpfCnpj.Create("11.222.333/0001-81");
    private static Email ValidEmail() => Email.Create("joao@example.com");
    private static Phone ValidPhone() => Phone.Create("11987654321");
    private static Address ValidAddress() => Address.Create("01310-100", "Av. Paulista", "1578", "Bela Vista", "São Paulo", "SP");

    // -------------------------------------------------------------------------
    // Pessoa Física — invariante de idade
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateIndividual_UnderAge_ThrowsDomainException()
    {
        // "hoje" = 2025-06-15 → nascido em 2010-01-01 tem 15 anos
        var birthDate = new DateOnly(2010, 1, 1);

        var act = () => Customer.CreateIndividual(
            "Menor de Idade", ValidCpf(), birthDate, ValidEmail(), ValidPhone(), ValidAddress(), Clock);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void CreateIndividual_ExactlyAge18_Succeeds()
    {
        // Nascido exatamente 18 anos antes de "hoje": 2007-06-15
        var birthDate = new DateOnly(2007, 6, 15);

        var customer = Customer.CreateIndividual(
            "João da Silva", ValidCpf(), birthDate, ValidEmail(), ValidPhone(), ValidAddress(), Clock);

        Assert.NotNull(customer);
        Assert.Equal(CustomerType.Individual, customer.Type);
    }

    [Fact]
    public void CreateIndividual_Over18_EmitsCustomerCreatedEvent()
    {
        var birthDate = new DateOnly(1990, 3, 10);

        var customer = Customer.CreateIndividual(
            "Maria Santos", ValidCpf(), birthDate, ValidEmail(), ValidPhone(), ValidAddress(), Clock);

        var evt = Assert.Single(customer.DomainEvents);
        var created = Assert.IsType<CustomerCreated>(evt);

        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(CustomerType.Individual, created.Type);
        Assert.Equal(customer.Email.Value, created.Email);
    }

    [Fact]
    public void CreateIndividual_OneDayBeforeTurning18_ThrowsDomainException()
    {
        // Aniversário amanhã: nascido em 2007-06-16 → ainda 17 anos em 2025-06-15
        var birthDate = new DateOnly(2007, 6, 16);

        var act = () => Customer.CreateIndividual(
            "Quase Adulto", ValidCpf(), birthDate, ValidEmail(), ValidPhone(), ValidAddress(), Clock);

        Assert.Throws<DomainException>(act);
    }

    // -------------------------------------------------------------------------
    // Pessoa Jurídica — invariante de Inscrição Estadual
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateCompany_NotExempt_WithoutIE_ThrowsDomainException()
    {
        var act = () => Customer.CreateCompany(
            "Empresa Ltda", ValidCnpj(), new DateOnly(2010, 5, 1),
            ValidEmail(), ValidPhone(), ValidAddress(),
            stateRegistration: null, isStateRegistrationExempt: false, Clock);

        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains("Inscrição Estadual", ex.Message);
    }

    [Fact]
    public void CreateCompany_Exempt_WithIE_ThrowsDomainException()
    {
        // Isento=true + IE preenchida = inconsistência
        var act = () => Customer.CreateCompany(
            "Empresa Ltda", ValidCnpj(), new DateOnly(2010, 5, 1),
            ValidEmail(), ValidPhone(), ValidAddress(),
            stateRegistration: "123.456.789.000", isStateRegistrationExempt: true, Clock);

        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains("isento", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateCompany_NotExempt_WithIE_Succeeds()
    {
        var customer = Customer.CreateCompany(
            "Empresa Ativa Ltda", ValidCnpj(), new DateOnly(2010, 5, 1),
            ValidEmail(), ValidPhone(), ValidAddress(),
            stateRegistration: "123.456.789.000", isStateRegistrationExempt: false, Clock);

        Assert.NotNull(customer);
        Assert.Equal(CustomerType.Company, customer.Type);
        Assert.False(customer.IsStateRegistrationExempt);
        Assert.NotNull(customer.StateRegistration);
    }

    [Fact]
    public void CreateCompany_Exempt_WithoutIE_Succeeds()
    {
        var customer = Customer.CreateCompany(
            "MEI Simples", ValidCnpj(), new DateOnly(2015, 8, 20),
            ValidEmail(), ValidPhone(), ValidAddress(),
            stateRegistration: null, isStateRegistrationExempt: true, Clock);

        Assert.True(customer.IsStateRegistrationExempt);
        Assert.Null(customer.StateRegistration);
    }

    [Fact]
    public void CreateCompany_EmitsCustomerCreatedEvent()
    {
        var customer = Customer.CreateCompany(
            "Empresa Ltda", ValidCnpj(), new DateOnly(2010, 5, 1),
            ValidEmail(), ValidPhone(), ValidAddress(),
            stateRegistration: "123.456.789.000", isStateRegistrationExempt: false, Clock);

        var evt = Assert.Single(customer.DomainEvents);
        Assert.IsType<CustomerCreated>(evt);
    }

    // -------------------------------------------------------------------------
    // Mutações e eventos
    // -------------------------------------------------------------------------

    [Fact]
    public void ChangeEmail_UpdatesEmailAndEmitsEvent()
    {
        var customer = BuildValidIndividual();
        customer.ClearDomainEvents();

        var newEmail = Email.Create("novo@email.com");
        customer.ChangeEmail(newEmail, Clock);

        Assert.Equal("novo@email.com", customer.Email.Value);

        var evt = Assert.Single(customer.DomainEvents);
        var changed = Assert.IsType<CustomerEmailChanged>(evt);
        Assert.Equal(customer.Id, changed.CustomerId);
        Assert.Equal("novo@email.com", changed.NewEmail);
    }

    [Fact]
    public void UpdateAddress_UpdatesAddressAndEmitsEvent()
    {
        var customer = BuildValidIndividual();
        customer.ClearDomainEvents();

        var newAddress = Address.Create("04538-133", "Av. Brigadeiro Faria Lima", "3477", "Itaim Bibi", "São Paulo", "SP");
        customer.UpdateAddress(newAddress, Clock);

        Assert.Equal("04538133", customer.Address.ZipCode);

        var evt = Assert.Single(customer.DomainEvents);
        var updated = Assert.IsType<CustomerAddressUpdated>(evt);
        Assert.Equal(customer.Id, updated.CustomerId);
        Assert.Equal("SP", updated.State);
    }

    [Fact]
    public void ChangePhone_UpdatesPhoneAndEmitsEvent()
    {
        var customer = BuildValidIndividual();
        customer.ClearDomainEvents();

        var newPhone = Phone.Create("11912345678");
        customer.ChangePhone(newPhone, Clock);

        Assert.Equal("11912345678", customer.Phone.Value);

        var evt = Assert.Single(customer.DomainEvents);
        Assert.IsType<CustomerPhoneChanged>(evt);
    }

    [Fact]
    public void UpdateTaxInfo_OnIndividual_ThrowsDomainException()
    {
        var customer = BuildValidIndividual();
        customer.ClearDomainEvents();

        var act = () => customer.UpdateTaxInfo("123456", false, Clock);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void UpdateTaxInfo_OnCompany_UpdatesAndEmitsEvent()
    {
        var customer = BuildValidCompany();
        customer.ClearDomainEvents();

        customer.UpdateTaxInfo("999.888.777.666", false, Clock);

        Assert.Equal("999.888.777.666", customer.StateRegistration);

        var evt = Assert.Single(customer.DomainEvents);
        Assert.IsType<CustomerTaxInfoUpdated>(evt);
    }

    // -------------------------------------------------------------------------
    // Value Objects — normalização
    // -------------------------------------------------------------------------

    [Fact]
    public void CpfCnpj_WithMask_StoresOnlyDigits()
    {
        var cpf = CpfCnpj.Create("529.982.247-25");

        Assert.Equal("52998224725", cpf.Value);
        Assert.True(cpf.IsCpf);
    }

    [Fact]
    public void CpfCnpj_Cnpj_WithMask_StoresOnlyDigits()
    {
        var cnpj = CpfCnpj.Create("11.222.333/0001-81");

        Assert.Equal("11222333000181", cnpj.Value);
        Assert.True(cnpj.IsCnpj);
    }

    [Fact]
    public void CpfCnpj_InvalidLength_ThrowsDomainException()
    {
        // 10 dígitos — nem CPF nem CNPJ
        Assert.Throws<DomainException>(() => CpfCnpj.Create("1234567890"));
    }

    [Fact]
    public void CpfCnpj_TooManyDigits_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CpfCnpj.Create("123456789012345")); // 15 dígitos
    }

    [Fact]
    public void Email_UpperCase_NormalizedToLowerCase()
    {
        var email = Email.Create("  JOAO@EXAMPLE.COM  ");

        Assert.Equal("joao@example.com", email.Value);
    }

    [Fact]
    public void Email_Invalid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Email.Create("nao-é-um-email"));
    }

    [Fact]
    public void Phone_WithPunctuation_StoresOnlyDigits()
    {
        var phone = Phone.Create("(11) 98765-4321");

        Assert.Equal("11987654321", phone.Value);
    }

    [Fact]
    public void Address_ZipCode_StoresOnlyDigits()
    {
        var address = Address.Create("01310-100", "Av. Paulista", "1578", "Bela Vista", "São Paulo", "SP");

        Assert.Equal("01310100", address.ZipCode);
    }

    [Fact]
    public void Address_State_NormalizedToUpperCase()
    {
        var address = Address.Create("01310100", "Rua X", "10", "Centro", "São Paulo", "sp");

        Assert.Equal("SP", address.State);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var customer = BuildValidIndividual();
        Assert.NotEmpty(customer.DomainEvents);

        customer.ClearDomainEvents();

        Assert.Empty(customer.DomainEvents);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Customer BuildValidIndividual() =>
        Customer.CreateIndividual(
            "João da Silva",
            CpfCnpj.Create("52998224725"),
            new DateOnly(1990, 3, 10),
            Email.Create("joao@example.com"),
            Phone.Create("11987654321"),
            Address.Create("01310100", "Av. Paulista", "1578", "Bela Vista", "São Paulo", "SP"),
            Clock);

    private static Customer BuildValidCompany() =>
        Customer.CreateCompany(
            "Empresa Ativa Ltda",
            CpfCnpj.Create("11222333000181"),
            new DateOnly(2010, 5, 1),
            Email.Create("empresa@example.com"),
            Phone.Create("1133334444"),
            Address.Create("01310100", "Av. Paulista", "1578", "Bela Vista", "São Paulo", "SP"),
            stateRegistration: "123.456.789.000",
            isStateRegistrationExempt: false,
            Clock);
}
