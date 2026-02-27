using CRM.Clients.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace CRM.Clients.IntegrationTests.Fixtures;

/// <summary>
/// Cria um container PostgreSQL isolado para a suite de testes.
/// Reutiliza o mesmo container em todos os testes da colecao (IAsyncLifetime).
///
/// Substitui o DbContext via ConfigureServices para garantir que a connection string
/// do Testcontainers prevaleca sobre appsettings.json em qualquer hosting model.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("crm_clients_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public HttpClient Client { get; private set; } = default!;

    private WebApplicationFactory<Program> _factory = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        string connectionString = _postgres.GetConnectionString();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Development");

                host.ConfigureServices(services =>
                {
                    // Remove o DbContext registrado por AddInfrastructure e substitui
                    // com a connection string do container de teste.
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<AppDbContext>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(connectionString));
                });
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
