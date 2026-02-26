using CRM.Clients.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Clients.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EventRecord> Events => Set<EventRecord>();

    public DbSet<CustomerReadModel> CustomerReadModels => Set<CustomerReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as IEntityTypeConfiguration<T> do assembly automaticamente.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
