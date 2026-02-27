using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Domain.Aggregates.Customer;
using MediatR;

namespace CRM.Clients.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers")
            .WithTags("Customers");

        group.MapPost("", CreateCustomer)
            .WithName("CreateCustomer")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("{id:guid}/email", ChangeEmail)
            .WithName("ChangeCustomerEmail")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("{id:guid}/address", UpdateAddress)
            .WithName("UpdateCustomerAddress")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateCustomer(
        CreateCustomerRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateCustomerCommand(
            request.Type,
            request.Name,
            request.Document,
            request.BirthOrFoundationDate,
            request.Email,
            request.Phone,
            request.ZipCode,
            request.Street,
            request.Number,
            request.District,
            request.City,
            request.State,
            request.StateRegistration,
            request.IsStateRegistrationExempt);

        Guid id = await sender.Send(command, ct);

        return Results.CreatedAtRoute("CreateCustomer", new { id }, id);
    }

    private static async Task<IResult> ChangeEmail(
        Guid id,
        ChangeEmailRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new ChangeCustomerEmailCommand(id, request.Email), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateAddress(
        Guid id,
        UpdateAddressRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new UpdateCustomerAddressCommand(
            id,
            request.ZipCode,
            request.Street,
            request.Number,
            request.District,
            request.City,
            request.State), ct);

        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // Request DTOs (locais ao endpoint -- sem dependencia de camada Application)
    // -------------------------------------------------------------------------

    private sealed record CreateCustomerRequest(
        CustomerType Type,
        string Name,
        string Document,
        DateOnly BirthOrFoundationDate,
        string Email,
        string Phone,
        string ZipCode,
        string Street,
        string Number,
        string District,
        string City,
        string State,
        string? StateRegistration,
        bool IsStateRegistrationExempt);

    private sealed record ChangeEmailRequest(string Email);

    private sealed record UpdateAddressRequest(
        string ZipCode,
        string Street,
        string Number,
        string District,
        string City,
        string State);
}
