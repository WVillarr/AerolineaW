using AerolineaW.Domain.Entities;
using AerolineaW.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AerolineaW.Infrastructure.Persistence.Configurations;

public sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code)
            .HasConversion(code => code.Value, value => new IataCode(value))
            .HasMaxLength(3)
            .IsRequired();
        builder.HasIndex(a => a.Code).IsUnique();

        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(100).IsRequired();
        builder.Property(a => a.TimeZoneId).HasMaxLength(100).IsRequired();
    }
}
