using CRM.Clients.Application.Common.Exceptions;
using CRM.Clients.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CRM.Clients.Api.Middleware;

public sealed partial class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
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
                LogValidationFailed(logger, ve.Message);
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status400BadRequest,
                    Title    = "Validation error",
                    Detail   = string.Join("; ", ve.Errors.Select(e => e.ErrorMessage)),
                    Instance = context.Request.Path,
                    Extensions = { ["errors"] = ve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
                };
                break;

            case DomainException de:
                LogDomainViolation(logger, de.Message);
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status400BadRequest,
                    Title    = "Domain rule violation",
                    Detail   = de.Message,
                    Instance = context.Request.Path
                };
                break;

            case NotFoundException nfe:
                LogNotFound(logger, nfe.Message);
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status404NotFound,
                    Title    = "Not found",
                    Detail   = nfe.Message,
                    Instance = context.Request.Path
                };
                break;

            case ConflictException cfe:
                LogConflict(logger, cfe.Message);
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status409Conflict,
                    Title    = "Conflict",
                    Detail   = cfe.Message,
                    Instance = context.Request.Path
                };
                break;

            case ConcurrencyException ce:
                LogConcurrencyConflict(logger, ce.Message);
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status409Conflict,
                    Title    = "Concurrency conflict",
                    Detail   = "O recurso foi modificado por outro processo. Tente novamente.",
                    Instance = context.Request.Path
                };
                break;

            case ServiceUnavailableException sue:
                LogServiceUnavailable(logger, sue.Message);
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status503ServiceUnavailable,
                    Title    = "Service unavailable",
                    Detail   = sue.Message,
                    Instance = context.Request.Path
                };
                break;

            default:
                LogUnhandledException(
                    logger, exception,
                    context.Request.Method, context.Request.Path.ToString());
                problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status500InternalServerError,
                    Title    = "Unexpected error",
                    Detail   = "An unexpected error occurred while processing the request.",
                    Instance = context.Request.Path
                };
                break;
        }

        context.Response.StatusCode  = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }

    // -------------------------------------------------------------------------
    // LoggerMessage delegates (CA1848) -- compilados em tempo de build pelo source generator.
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "Validation failed: {Errors}")]
    private static partial void LogValidationFailed(ILogger logger, string errors);

    [LoggerMessage(Level = LogLevel.Information, Message = "Domain rule violation: {Message}")]
    private static partial void LogDomainViolation(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resource not found: {Message}")]
    private static partial void LogNotFound(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Conflict: {Message}")]
    private static partial void LogConflict(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrency conflict: {Message}")]
    private static partial void LogConcurrencyConflict(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Service unavailable: {Message}")]
    private static partial void LogServiceUnavailable(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(
        ILogger logger, Exception exception, string method, string path);
}
