using CRM.Clients.Application.Customers.Queries;
using CRM.Clients.Application.Customers.Queries.Models;
using MediatR;

namespace CRM.Clients.Api.Endpoints;

public static class AddressEndpoints
{
    public static IEndpointRouteBuilder MapAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/addresses")
            .WithTags("Addresses");

        group.MapGet("by-zipcode/{zipCode}", GetByZipCode)
            .WithName("GetAddressByZipCode")
            .WithSummary("Consulta endereco pelo CEP via servico externo (ViaCEP).")
            .Produces<ViaCepAddressDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetByZipCode(
        string zipCode,
        ISender sender,
        CancellationToken ct)
    {
        ViaCepAddressDto? dto = await sender.Send(new GetAddressByZipCodeQuery(zipCode), ct);

        return dto is null
            ? Results.NotFound(new { detail = $"CEP '{zipCode}' nao encontrado." })
            : Results.Ok(dto);
    }
}
