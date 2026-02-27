using CRM.Clients.Application.Abstractions;
using CRM.Clients.Domain.Abstractions;
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

        // Read side: repositorio de consulta separado do write side.
        services.AddScoped<ICustomerReadRepository, EfCustomerReadRepository>();

        return services;
    }
}
