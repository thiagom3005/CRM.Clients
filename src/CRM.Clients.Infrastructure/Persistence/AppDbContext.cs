using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // TODO: adicionar mapeamentos e estratégia de Event Store quando o desenho de domínio estiver estabilizado.
}
