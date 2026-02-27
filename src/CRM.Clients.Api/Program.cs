using System.Globalization;
using CRM.Clients.Api.Endpoints;
using CRM.Clients.Api.Middleware;
using CRM.Clients.Application;
using CRM.Clients.Infrastructure;
using CRM.Clients.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => _ = loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            formatProvider: CultureInfo.InvariantCulture,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {UserId} {Message:lj}{NewLine}{Exception}"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "CRM.Clients API",
        Version     = "v1",
        Description = "Modulo de clientes: DDD + CQRS + Event Sourcing."
    });
});

builder.Services.AddApplication();
// AddInfrastructure ja registra AddHealthChecks + PostgresHealthCheck.
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

// Migrations automaticas em Development — prod usa pipeline de deploy separado.
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

// CorrelationId antes de tudo: garante que erros ja tenham contexto de rastreio.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

// /health/live — processo vivo? Nao verifica dependencias. Usado pelo liveness probe.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false   // nenhum check: responde 200 se o processo esta de pe
});

// /health/ready — banco acessivel? Usado pelo readiness probe antes de receber trafego.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapCustomerEndpoints();
app.MapAddressEndpoints();

#pragma warning disable CA1848 // LoggerMessage delegates nao sao viaveis em top-level statements
app.Logger.LogInformation(
    "CRM.Clients iniciado. Ambiente: {Env} | Swagger: {Swagger}",
    app.Environment.EnvironmentName,
    app.Environment.IsDevelopment() ? "http://localhost/swagger" : "desabilitado");
#pragma warning restore CA1848

app.Run();

// Necessario para WebApplicationFactory nos testes de integracao.
public partial class Program { }
