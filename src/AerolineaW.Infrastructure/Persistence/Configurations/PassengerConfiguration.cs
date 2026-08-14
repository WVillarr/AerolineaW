using AerolineaW.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AerolineaW.Infrastructure.Persistence.Configurations;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.DocumentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.DocumentNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Country).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(30).IsRequired();

        // Not one of the plan's originally listed unique indexes, but the same document
        // shouldn't be able to check in as two different passenger records.
        builder.HasIndex(p => new { p.DocumentType, p.DocumentNumber }).IsUnique();
    }
}
