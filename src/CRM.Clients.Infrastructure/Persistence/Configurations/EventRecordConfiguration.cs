using CRM.Clients.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Clients.Infrastructure.Persistence.Configurations;

public sealed class EventRecordConfiguration : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AggregateId)
            .IsRequired();

        builder.Property(e => e.AggregateType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsRequired();

        // jsonb permite consultas e indices parciais no Postgres.
        builder.Property(e => e.Data)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.OccurredAtUtc)
            .IsRequired();

        // Unicidade (AggregateId, Version): se dois processos tentarem gravar a mesma versao,
        // o banco rejeita com unique_violation (23505) -- base da concorrencia otimista.
        builder.HasIndex(e => new { e.AggregateId, e.Version })
            .IsUnique()
            .HasDatabaseName("ix_events_aggregate_version");

        builder.HasIndex(e => e.AggregateId)
            .HasDatabaseName("ix_events_aggregate_id");
    }
}
