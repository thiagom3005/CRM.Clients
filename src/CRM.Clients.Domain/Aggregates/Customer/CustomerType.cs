namespace CRM.Clients.Domain.Aggregates.Customer;

/// <summary>
/// Classifica o cliente como Pessoa Fisica ou Juridica.
/// Determina quais invariantes de negocio sao aplicadas no aggregate.
/// </summary>
public enum CustomerType
{
    /// <summary>Pessoa Fisica -- exige CPF e valida idade minima de 18 anos.</summary>
    Individual = 1,

    /// <summary>Pessoa Juridica -- exige CNPJ e Inscricao Estadual ou isencao declarada.</summary>
    Company = 2
}
