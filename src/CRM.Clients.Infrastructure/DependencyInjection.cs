using CRM.Clients.Application.Abstractions;
using CRM.Clients.Domain.Abstractions;
using CRM.Clients.Infrastructure.ExternalServices;
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

        // IClock: singleton pois SystemClock nao tem estado.
        services.AddSingleton<IClock, SystemClock>();

        // Repositorios: scoped para compartilhar o mesmo DbContext dentro da request.
        services.AddScoped<IEventStore, PgEventStore>();
        services.AddScoped<ICustomerProjectionWriter, CustomerProjectionWriter>();
        services.AddScoped<ICustomerReadModelReader, CustomerReadModelReader>();
        services.AddScoped<ICustomerReadRepository, EfCustomerReadRepository>();
        services.AddScoped<IEventHistoryReader, EfEventHistoryReader>();

        // Contexto de execucao: le X-User e X-Correlation-Id do HttpContext.
        services.AddHttpContextAccessor();
        services.AddScoped<IExecutionContextAccessor, HttpExecutionContextAccessor>();

        // ViaCEP: HttpClient com politicas Polly de resiliencia.
        // Retry exponencial 3x + Timeout 3s + Circuit Breaker (5 falhas / 30s).
        services.AddHttpClient<IViaCepClient, ViaCepClient>(client =>
            {
                client.BaseAddress = new Uri("https://viacep.com.br/");
                client.Timeout = TimeSpan.FromSeconds(10); // timeout global do handler
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);

                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);

                options.CircuitBreaker.SamplingDuration    = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput   = 5;
                options.CircuitBreaker.FailureRatio        = 0.5;
                options.CircuitBreaker.BreakDuration       = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
