using CRM.Clients.Api.Middleware;
using CRM.Clients.Application;
using CRM.Clients.Infrastructure;
using FluentValidation;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Log estruturado ajuda a diagnosticar problemas sem perder contexto entre servicos.
builder.Host.UseSerilog((context, services, loggerConfiguration) => _ = loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger disponivel apenas em Development para nao expor metadados em producao.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.MapHealthChecks("/health/live");

app.MapGet("/health", () =>
    // Endpoint simples para monitoramento externo (status + metadados basicos).
    Results.Ok(new
    {
        status = "ok",
        service = "CRM.Clients",
        timestamp = DateTime.UtcNow
    }))
.WithName("Health")
.WithTags("Health");

app.Run();
