using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Application.Customers.Queries;
using CRM.Clients.Application.Customers.Queries.Models;
using CRM.Clients.Domain.Aggregates.Customer;
using MediatR;

namespace CRM.Clients.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers")
            .WithTags("Customers");

        // ---- Read ----

        group.MapGet("{id:guid}", GetById)
            .WithName("GetCustomerById")
            .WithSummary("Retorna o detalhamento completo de um cliente.")
            .Produces<CustomerDetailsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("", Search)
            .WithName("SearchCustomers")
            .WithSummary("Busca paginada de clientes por nome, e-mail ou documento.")
            .WithDescription(
                "Parametros de sort aceitos: nameAsc, nameDesc, updatedAtAsc, updatedAtDesc (default).")
            .Produces<PagedResult<CustomerListItemDto>>(StatusCodes.Status200OK);

        group.MapGet("{id:guid}/events", GetEvents)
            .WithName("GetCustomerEvents")
            .WithSummary("Retorna o historico paginado de eventos de um cliente.")
            .Produces<PagedResult<CustomerEventDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Write ----

        // Location aponta para GetCustomerById apos criacao.
        group.MapPost("", CreateCustomer)
            .WithName("CreateCustomer")
            .WithSummary("Cria um novo cliente (PF ou PJ).")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("{id:guid}/email", ChangeEmail)
            .WithName("ChangeCustomerEmail")
            .WithSummary("Altera o e-mail de contato do cliente.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("{id:guid}/address", UpdateAddress)
            .WithName("UpdateCustomerAddress")
            .WithSummary("Atualiza o endereco do cliente.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    // -------------------------------------------------------------------------
    // Handlers de read
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetById(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetCustomerByIdQuery(id), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> Search(
        ISender sender,
        CancellationToken ct,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        string? sort = null)
    {
        var result = await sender.Send(
            new SearchCustomersQuery(search, page, pageSize, sort), ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetEvents(
        Guid id,
        ISender sender,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetCustomerEventsQuery(id, page, pageSize), ct);
        return Results.Ok(result);
    }

    // -------------------------------------------------------------------------
    // Handlers de write
    // -------------------------------------------------------------------------

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

        // Location header aponta para o GET de detalhamento.
        return Results.CreatedAtRoute("GetCustomerById", new { id }, id);
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
