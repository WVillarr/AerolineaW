using AerolineaW.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AerolineaW.Infrastructure.Persistence.Configurations;

public sealed class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Registration).HasMaxLength(20).IsRequired();
        builder.HasIndex(a => a.Registration).IsUnique();

        builder.Property(a => a.Model).HasMaxLength(100).IsRequired();
        builder.Property(a => a.MaxPayloadKg).HasPrecision(10, 2);

        // Stored as text rather than the numeric enum value so the raw table is legible
        // without a lookup table — worth the few extra bytes for a project this size.
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
    }
}
