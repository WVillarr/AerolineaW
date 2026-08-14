using AerolineaW.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AerolineaW.Infrastructure.Persistence.Configurations;

public sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FlightNumber).HasMaxLength(10).IsRequired();
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.PayloadBookedKg).HasPrecision(10, 2);

        // The concurrency token the flagship "N simultaneous bookings, exactly SeatCapacity
        // succeed" scenario depends on: every Flight.Book() call increments SeatsBooked, which
        // means every booking issues an UPDATE against this row, so two racing bookings for
        // different seats produce a real conflict here instead of silently overselling.
        builder.Property(f => f.RowVersion).IsRowVersion();

        builder.HasOne(f => f.Aircraft)
            .WithMany()
            .HasForeignKey(f => f.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.OriginAirport)
            .WithMany()
            .HasForeignKey(f => f.OriginAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.DestinationAirport)
            .WithMany()
            .HasForeignKey(f => f.DestinationAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bookings is IReadOnlyCollection<Booking> with no public mutator, backed by the
        // private _bookings field — EF has to write to that field directly, bypassing the
        // property, since there's no Add/Remove for it to call.
        builder.HasMany(f => f.Bookings)
            .WithOne(b => b.Flight)
            .HasForeignKey(b => b.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(f => f.Bookings)
            .HasField("_bookings")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(f => f.AircraftId);
    }
}
