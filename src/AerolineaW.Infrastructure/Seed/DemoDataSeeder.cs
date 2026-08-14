using AerolineaW.Domain.Common;
using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;
using AerolineaW.Domain.ValueObjects;
using AerolineaW.Infrastructure.Persistence;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AerolineaW.Infrastructure.Seed;

/// <summary>
/// Populates the database with a realistic, internally-consistent demo dataset on startup —
/// registered only for Development (see <see cref="DependencyInjection.AddInfrastructure"/>).
/// Every entity is built through its real constructor and behavior methods (<see cref="Flight.Book"/>,
/// <see cref="Flight.Board"/>, etc.), never inserted as raw rows, so the seed data is guaranteed
/// to satisfy the same domain invariants as data created through the API.
/// </summary>
public sealed class DemoDataSeeder(IServiceScopeFactory scopeFactory, ILogger<DemoDataSeeder> logger) : IHostedService
{
    private const int Seed = 20260813;

    private static readonly (string Code, string Name, string City, string Country, string TimeZoneId)[] AirportSeed =
    [
        ("MIA", "Miami International", "Miami", "United States", "America/New_York"),
        ("JFK", "John F. Kennedy International", "New York", "United States", "America/New_York"),
        ("LAX", "Los Angeles International", "Los Angeles", "United States", "America/Los_Angeles"),
        ("ORD", "O'Hare International", "Chicago", "United States", "America/Chicago"),
        ("LHR", "Heathrow", "London", "United Kingdom", "Europe/London"),
        ("CDG", "Charles de Gaulle", "Paris", "France", "Europe/Paris"),
        ("MAD", "Adolfo Suarez Madrid-Barajas", "Madrid", "Spain", "Europe/Madrid"),
        ("GRU", "Sao Paulo-Guarulhos", "Sao Paulo", "Brazil", "America/Sao_Paulo"),
        ("BOG", "El Dorado International", "Bogota", "Colombia", "America/Bogota"),
        ("NRT", "Narita International", "Tokyo", "Japan", "Asia/Tokyo"),
    ];

    private static readonly (string Model, int SeatCapacity, decimal MaxPayloadKg)[] AircraftModels =
    [
        ("Boeing 737-800", 189, 20000m),
        ("Airbus A320neo", 180, 19000m),
        ("Boeing 787-9", 296, 34000m),
        ("Airbus A350-900", 325, 38000m),
        ("Embraer E190", 100, 12000m),
        ("Boeing 777-300ER", 396, 45000m),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Airports.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Demo data already present; skipping seed");
            return;
        }

        logger.LogInformation("Seeding demo data...");

        var faker = new Faker { Random = new Randomizer(Seed) };

        var airports = AirportSeed
            .Select(a => new Airport(new IataCode(a.Code), a.Name, a.City, a.Country, a.TimeZoneId))
            .ToList();

        var aircraft = SeedAircraft(faker);
        var passengers = SeedPassengers(faker);
        var flights = SeedFlights(faker, aircraft, airports);

        SeedBookings(faker, flights, passengers);
        AdvanceFlightLifecycles(flights);

        db.Airports.AddRange(airports);
        db.Aircraft.AddRange(aircraft);
        db.Passengers.AddRange(passengers);
        db.Flights.AddRange(flights); // pulls in every seeded Booking too, via the Bookings navigation

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Airports} airports, {Aircraft} aircraft, {Passengers} passengers, {Flights} flights",
            airports.Count, aircraft.Count, passengers.Count, flights.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static List<Aircraft> SeedAircraft(Faker faker)
    {
        var aircraft = new List<Aircraft>();
        for (var i = 0; i < AircraftModels.Length; i++)
        {
            var (model, seatCapacity, maxPayloadKg) = AircraftModels[i];
            var registration = $"N{faker.Random.Number(100, 999)}{(char)('A' + i)}W";
            aircraft.Add(new Aircraft(registration, model, seatCapacity, maxPayloadKg));
        }

        return aircraft;
    }

    private static List<Passenger> SeedPassengers(Faker faker)
    {
        var passengerFaker = new Faker<Passenger>()
            .CustomInstantiator(f => new Passenger(
                f.Person.FullName,
                f.PickRandom<DocumentType>(),
                f.Random.Replace("??######").ToUpperInvariant(),
                f.Address.Country(),
                f.Phone.PhoneNumber()));

        return passengerFaker.Generate(40);
    }

    private static List<Flight> SeedFlights(Faker faker, List<Aircraft> aircraft, List<Airport> airports)
    {
        var flights = new List<Flight>();

        foreach (var plane in aircraft)
        {
            // Stagger each aircraft's schedule across a two-week window centered on "now", so
            // the seeded data ends up with a realistic mix of completed and upcoming flights
            // rather than everything landing on the same side of "today".
            var departure = DateTime.UtcNow.Date
                .AddDays(faker.Random.Number(-10, 3))
                .AddHours(faker.Random.Number(0, 18));

            for (var leg = 0; leg < 4; leg++)
            {
                var (origin, destination) = PickRoute(faker, airports);
                var arrival = departure.AddHours(faker.Random.Double(1.5, 4));

                flights.Add(new Flight($"AW{faker.Random.Number(100, 999)}", plane, origin, destination, departure, arrival));

                // Comfortably clears Flight.TurnaroundBuffer (45 minutes) between legs.
                departure = arrival.AddHours(6);
            }
        }

        return flights;
    }

    private static (Airport Origin, Airport Destination) PickRoute(Faker faker, List<Airport> airports)
    {
        var origin = faker.PickRandom(airports);
        Airport destination;
        do
        {
            destination = faker.PickRandom(airports);
        }
        while (destination.Code == origin.Code);

        return (origin, destination);
    }

    private static void SeedBookings(Faker faker, List<Flight> flights, List<Passenger> passengers)
    {
        foreach (var flight in flights)
        {
            var seatsToFill = faker.Random.Number(0, (int)(flight.Aircraft.SeatCapacity * 0.4));
            var bookedAt = flight.ScheduledDepartureUtc.AddDays(-faker.Random.Number(1, 20));
            var usedSeats = new HashSet<string>();

            for (var i = 0; i < seatsToFill; i++)
            {
                var seat = GenerateUnusedSeat(faker, flight.Aircraft.SeatCapacity, usedSeats);
                var passenger = faker.PickRandom(passengers);
                var baggage = faker.Random.Decimal(0, 23);
                var price = faker.Random.Decimal(80, 650);

                try
                {
                    flight.Book(passenger, seat, baggage, price, bookedAt);
                }
                catch (DomainException)
                {
                    // Random data occasionally collides with a rule (payload, capacity): skip
                    // that seat. The demo dataset doesn't need to be perfectly full.
                }
            }
        }
    }

    private static SeatNumber GenerateUnusedSeat(Faker faker, int seatCapacity, HashSet<string> usedSeats)
    {
        var rows = Math.Max(1, seatCapacity / 6);
        string candidate;
        do
        {
            var row = faker.Random.Number(1, rows);
            var letter = faker.PickRandom('A', 'B', 'C', 'D', 'E', 'F');
            candidate = $"{row}{letter}";
        }
        while (!usedSeats.Add(candidate));

        return new SeatNumber(candidate);
    }

    private static void AdvanceFlightLifecycles(List<Flight> flights)
    {
        var now = DateTime.UtcNow;
        var pastFlights = flights.Where(f => f.ScheduledArrivalUtc < now).ToList();
        var futureFlights = flights.Where(f => f.ScheduledArrivalUtc >= now).ToList();

        // Every flight that has already happened completed its lifecycle right on schedule.
        foreach (var flight in pastFlights)
        {
            flight.Board();
            flight.Depart(flight.ScheduledDepartureUtc);
            flight.Land(flight.ScheduledArrivalUtc);
        }

        // A couple of illustrative exceptions among the still-upcoming flights, so the seeded
        // data shows every FlightStatus worth seeing in Swagger, not just Scheduled and Landed.
        if (futureFlights.Count > 0)
        {
            futureFlights[0].Cancel();
        }

        if (futureFlights.Count > 1)
        {
            futureFlights[1].Delay();
        }
    }
}
