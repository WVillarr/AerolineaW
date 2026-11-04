using AerolineaW.Domain.Entities;
using AerolineaW.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AerolineaW.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Reference)
            .HasConversion(r => r.Value, value => new BookingReference(value))
            .HasMaxLength(6)
            .IsRequired();
        builder.HasIndex(b => b.Reference).IsUnique();

        builder.Property(b => b.Seat)
            .HasConversion(s => s.Value, value => new SeatNumber(value))
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(b => b.BaggageWeightKg).HasPrecision(10, 2);
        builder.Property(b => b.Price).HasPrecision(10, 2);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasOne(b => b.Passenger)
            .WithMany()
            .HasForeignKey(b => b.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        // The Flight/FlightId side of this relationship is configured from FlightConfiguration
        // (HasMany(f => f.Bookings)) — EF only needs it declared once.
        builder.HasIndex(b => new { b.FlightId, b.Seat }).IsUnique()
            .HasFilter("[Status] <> 'Cancelled'");
    }
}
