using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries;

/// <summary>Consulta CEP no servico externo ViaCEP e retorna o endereco formatado.</summary>
/// <param name="ZipCode">CEP com ou sem formatacao — handler normaliza para somente digitos.</param>
public sealed record GetAddressByZipCodeQuery(string ZipCode) : IRequest<ViaCepAddressDto?>;
