namespace CRM.Clients.Domain.Aggregates.Customer;

/// <summary>
/// Classifica o cliente como Pessoa Física ou Jurídica.
/// Determina quais invariantes de negócio são aplicadas no aggregate.
/// </summary>
public enum CustomerType
{
    /// <summary>Pessoa Física — exige CPF e valida idade mínima de 18 anos.</summary>
    Individual = 1,

    /// <summary>Pessoa Jurídica — exige CNPJ e Inscrição Estadual ou isenção declarada.</summary>
    Company = 2
}
