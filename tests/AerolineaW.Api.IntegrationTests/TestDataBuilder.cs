using System.Net.Http.Json;
using AerolineaW.Application.Aircraft;
using AerolineaW.Application.Airports;
using AerolineaW.Application.Flights;
using AerolineaW.Application.Passengers;
using AerolineaW.Domain.Enums;

namespace AerolineaW.Api.IntegrationTests;

/// <summary>
/// Builds fixture data through the real HTTP endpoints rather than by touching the database
/// directly — every test's arrange step is itself a live exercise of the create endpoints it
/// depends on. Every value is randomized so tests sharing one container-wide database (see
/// <see cref="SharedApiFixture"/>) never collide with each other's rows.
/// </summary>
internal static class TestDataBuilder
{
    public static async Task<AirportResponse> CreateAirportAsync(HttpClient client)
    {
        var code = RandomLetters(3);
        var response = await client.PostAsJsonAsync(
            "/api/v1/airports", new CreateAirportRequest(code, $"{code} Airport", "City", "Country", "America/New_York"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AirportResponse>())!;
    }

    public static async Task<AircraftResponse> CreateAircraftAsync(HttpClient client, int seatCapacity = 150, decimal maxPayloadKg = 15_000m)
    {
        var registration = $"N{Random.Shared.Next(10_000, 99_999)}{RandomLetters(1)}";
        var response = await client.PostAsJsonAsync(
            "/api/v1/aircraft", new CreateAircraftRequest(registration, "Test Model", seatCapacity, maxPayloadKg));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AircraftResponse>())!;
    }

    public static async Task<PassengerResponse> CreatePassengerAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var response = await client.PostAsJsonAsync(
            "/api/v1/passengers",
            new CreatePassengerRequest($"Test Passenger {suffix}", DocumentType.Passport, $"DOC{suffix}", "Country", "+1 555 0100"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PassengerResponse>())!;
    }

    public static async Task<FlightResponse> CreateFlightAsync(
        HttpClient client, int aircraftId, int originAirportId, int destinationAirportId, DateTime departureUtc, TimeSpan? duration = null)
    {
        var request = new CreateFlightRequest(
            $"FL{Random.Shared.Next(100, 999)}",
            aircraftId,
            originAirportId,
            destinationAirportId,
            departureUtc,
            departureUtc + (duration ?? TimeSpan.FromHours(2)));

        var response = await client.PostAsJsonAsync("/api/v1/flights", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FlightResponse>())!;
    }

    private static string RandomLetters(int count) =>
        new([.. Enumerable.Range(0, count).Select(_ => (char)('A' + Random.Shared.Next(26)))]);
}
