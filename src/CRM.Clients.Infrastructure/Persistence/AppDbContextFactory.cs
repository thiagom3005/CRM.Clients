using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.Clients.Infrastructure.Persistence;

/// <summary>
/// Factory usada pelo EF Core CLI (dotnet ef) em design-time para criar migrations
/// sem precisar iniciar o host da aplicacao.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // String de conexao local para design-time; em producao e injetada via config.
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=crm_clients;Username=postgres;Password=postgres");

        return new AppDbContext(optionsBuilder.Options);
    }
}
