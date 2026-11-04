using AerolineaW.Application.Bookings;
using AerolineaW.Domain.Enums;
using AerolineaW.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class MigrationTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task UpgradeInitialDatabase_PreservesBookingsAndEnablesSeatReuse()
    {
        var connection = new SqlConnectionStringBuilder(factory.ConnectionString)
        {
            InitialCatalog = $"AerolineaW_MigrationTests_{Guid.NewGuid():N}",
        }.ConnectionString;
        await using var db = new AirlineDbContext(new DbContextOptionsBuilder<AirlineDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20260814000800_InitialCreate");
            // Use the old wire schema: the current EF model already expects Booking.RowVersion.
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO Airports (Code, Name, City, Country, TimeZoneId) VALUES
                    ('AAA', 'Origin', 'City', 'Country', 'America/New_York'),
                    ('BBB', 'Destination', 'City', 'Country', 'America/New_York');
                INSERT INTO Aircraft (Registration, Model, SeatCapacity, MaxPayloadKg, Status)
                    VALUES ('MIGRATION', 'Test', 6, 1000, 'InService');
                INSERT INTO Passengers (FullName, DocumentType, DocumentNumber, Country, Phone)
                    VALUES ('Existing Passenger', 'Passport', 'MIGRATION', 'Country', '123');
                INSERT INTO Flights (FlightNumber, AircraftId, OriginAirportId, DestinationAirportId,
                    ScheduledDepartureUtc, ScheduledArrivalUtc, Status, SeatsBooked, PayloadBookedKg)
                    VALUES ('MIGRATE', 1, 1, 2, DATEADD(day, 7, SYSUTCDATETIME()),
                        DATEADD(hour, 170, SYSUTCDATETIME()), 'Scheduled', 1, 10);
                INSERT INTO Bookings (Reference, FlightId, PassengerId, Seat, BaggageWeightKg, Price, Status, BookedAtUtc)
                    VALUES ('ABC234', 1, 1, '1A', 10, 100, 'Confirmed', SYSUTCDATETIME());
                """);

            await db.Database.MigrateAsync();
            var preserved = await db.Bookings.SingleAsync();
            Assert.Equal("ABC234", preserved.Reference.Value);
            Assert.Equal(8, preserved.RowVersion.Length);
            var service = new BookingService(db);
            await service.CancelAsync(preserved.Id);
            await service.CreateAsync(1, new CreateBookingRequest(1, "1A", 15m, 120m));
            db.ChangeTracker.Clear();
            var history = await db.Bookings.OrderBy(b => b.Id).ToListAsync();
            Assert.Equal(2, history.Count);
            Assert.Equal(BookingStatus.Cancelled, history[0].Status);
            Assert.Equal(BookingStatus.Confirmed, history[1].Status);
            var flight = await db.Flights.SingleAsync();
            Assert.Equal(1, flight.SeatsBooked);
            Assert.Equal(15m, flight.PayloadBookedKg);
            Assert.False(db.Database.HasPendingModelChanges());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
