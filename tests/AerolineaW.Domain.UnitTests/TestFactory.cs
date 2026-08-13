using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;
using AerolineaW.Domain.ValueObjects;

namespace AerolineaW.Domain.UnitTests;

/// <summary>Shared builders for valid domain objects, so each test only sets up what it varies.</summary>
internal static class TestFactory
{
    public static Airport CreateAirport(string code = "MIA", string timeZoneId = "America/New_York") =>
        new(new IataCode(code), $"{code} International", "Some City", "Some Country", timeZoneId);

    public static Aircraft CreateAircraft(int seatCapacity = 150, decimal maxPayloadKg = 2000m) =>
        new("N12345", "Boeing 737", seatCapacity, maxPayloadKg);

    public static Passenger CreatePassenger(string documentNumber = "AB123456") =>
        new("Jane Doe", DocumentType.Passport, documentNumber, "Some Country", "+1 555 0100");

    public static Flight CreateFlight(
        Aircraft? aircraft = null,
        Airport? origin = null,
        Airport? destination = null,
        DateTime? departureUtc = null,
        DateTime? arrivalUtc = null,
        string flightNumber = "AW100")
    {
        var departure = departureUtc ?? DateTime.UtcNow.AddDays(1);
        var arrival = arrivalUtc ?? departure.AddHours(2);

        return new Flight(
            flightNumber,
            aircraft ?? CreateAircraft(),
            origin ?? CreateAirport(),
            destination ?? CreateAirport("JFK"),
            departure,
            arrival);
    }
}
