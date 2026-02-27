using CRM.Clients.Application.Abstractions;
using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Application.Customers.Queries;

/// <summary>
/// Handler que delega a consulta de CEP ao IViaCepClient (Infrastructure).
/// Normaliza o CEP para apenas digitos antes de repassar ao cliente HTTP.
/// </summary>
public sealed class GetAddressByZipCodeQueryHandler(IViaCepClient viaCepClient)
    : IRequestHandler<GetAddressByZipCodeQuery, ViaCepAddressDto?>
{
    public Task<ViaCepAddressDto?> Handle(
        GetAddressByZipCodeQuery request,
        CancellationToken cancellationToken)
    {
        string digits = new string(request.ZipCode.Where(char.IsDigit).ToArray());
        return viaCepClient.GetAddressAsync(digits, cancellationToken);
    }
}
