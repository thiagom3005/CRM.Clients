using CRM.Clients.Application;
using CRM.Clients.Api.Middleware;
using CRM.Clients.Infrastructure;
using FluentValidation;
using MediatR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logs estruturados facilitam correlação entre serviços e aceleram investigação de incidentes em ambientes distribuídos.
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health/live");

app.MapGet("/health", () =>
{
    // Este endpoint será consumido por orquestradores e ferramentas de monitoramento para validar disponibilidade básica.
    return Results.Ok(new
    {
        status = "ok",
        service = "CRM.Clients",
        timestamp = DateTime.UtcNow
    });
})
.WithName("Health")
.WithTags("Health");

app.Run();
