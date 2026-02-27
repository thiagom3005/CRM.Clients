using CRM.Clients.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Clients.Infrastructure.Persistence.Configurations;

public sealed class CustomerReadModelConfiguration : IEntityTypeConfiguration<CustomerReadModel>
{
    public void Configure(EntityTypeBuilder<CustomerReadModel> builder)
    {
        builder.ToTable("customer_read_model");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Document)
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(13);

        builder.Property(c => c.ZipCode)
            .HasMaxLength(8);

        builder.Property(c => c.Street)
            .HasMaxLength(200);

        builder.Property(c => c.Number)
            .HasMaxLength(20);

        builder.Property(c => c.District)
            .HasMaxLength(100);

        builder.Property(c => c.City)
            .HasMaxLength(100);

        builder.Property(c => c.State)
            .HasMaxLength(2);

        builder.Property(c => c.StateRegistration)
            .HasMaxLength(50);

        // Indices UNIQUE garantem integridade mesmo em cenarios de concorrencia.
        builder.HasIndex(c => c.Document)
            .IsUnique()
            .HasDatabaseName("ix_customer_read_model_document");

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("ix_customer_read_model_email");
    }
}
