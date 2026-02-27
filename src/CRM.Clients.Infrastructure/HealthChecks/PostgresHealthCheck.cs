using CRM.Clients.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRM.Clients.Infrastructure.HealthChecks;

// Readiness check: verifica se o banco esta acessivel antes de aceitar trafego.
public sealed class PostgresHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres inacessivel.", ex);
        }
    }
}
