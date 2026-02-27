using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Clients.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException ve:
                logger.LogInformation("Validation failed: {Errors}", ve.Message);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation error",
                    Detail = string.Join("; ", ve.Errors.Select(e => e.ErrorMessage)),
                    Instance = context.Request.Path,
                    Extensions = { ["errors"] = ve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
                };
                break;

            case DomainException de:
                logger.LogInformation("Domain rule violation: {Message}", de.Message);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Domain rule violation",
                    Detail = de.Message,
                    Instance = context.Request.Path
                };
                break;

            case NotFoundException nfe:
                logger.LogInformation("Resource not found: {Message}", nfe.Message);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not found",
                    Detail = nfe.Message,
                    Instance = context.Request.Path
                };
                break;

            case ConflictException cfe:
                logger.LogInformation("Conflict: {Message}", cfe.Message);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = cfe.Message,
                    Instance = context.Request.Path
                };
                break;

            case ConcurrencyException ce:
                logger.LogWarning("Concurrency conflict: {Message}", ce.Message);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrency conflict",
                    Detail = "O recurso foi modificado por outro processo. Tente novamente.",
                    Instance = context.Request.Path
                };
                break;

            default:
                logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Unexpected error",
                    Detail = "An unexpected error occurred while processing the request.",
                    Instance = context.Request.Path
                };
                break;
        }

        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
