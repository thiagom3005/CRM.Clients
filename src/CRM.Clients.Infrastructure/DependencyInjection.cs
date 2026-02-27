using CRM.Clients.Application.Abstractions;
using CRM.Clients.Domain.Abstractions;
using CRM.Clients.Infrastructure.ExternalServices;
using CRM.Clients.Infrastructure.HealthChecks;
using CRM.Clients.Infrastructure.Http;
using CRM.Clients.Infrastructure.Persistence;
using CRM.Clients.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Clients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' nao configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IClock, SystemClock>();

        // Repositorios scoped: compartilham o mesmo DbContext dentro da request.
        services.AddScoped<IEventStore, PgEventStore>();
        services.AddScoped<ICustomerProjectionWriter, CustomerProjectionWriter>();
        services.AddScoped<ICustomerReadModelReader, CustomerReadModelReader>();
        services.AddScoped<ICustomerReadRepository, EfCustomerReadRepository>();
        services.AddScoped<IEventHistoryReader, EfEventHistoryReader>();

        // MVP: usuario vem do header X-User; em producao viria do token JWT.
        services.AddHttpContextAccessor();
        services.AddScoped<IExecutionContextAccessor, HttpExecutionContextAccessor>();

        // Integracao externa falha — retry + circuit breaker evitam que o ViaCEP derrube o dominio.
        services.AddHttpClient<IViaCepClient, ViaCepClient>(client =>
            {
                client.BaseAddress = new Uri("https://viacep.com.br/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);

                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.AttemptTimeout.Timeout      = TimeSpan.FromSeconds(3);

                options.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.FailureRatio      = 0.5;
                options.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(30);
            });

        // Readiness check tagueado para separar /health/live de /health/ready.
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

        return services;
    }
}
