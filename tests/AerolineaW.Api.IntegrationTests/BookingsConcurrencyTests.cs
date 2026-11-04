using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Bookings;
using AerolineaW.Application.Flights;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class BookingsConcurrencyTests(ApiWebApplicationFactory factory)
{
    /// <summary>
    /// The flagship scenario the whole optimistic-concurrency design exists for: fire more
    /// simultaneous booking requests than the aircraft has seats and confirm no overselling.
    /// Three server attempts may exhaust under contention even with capacity remaining;
    /// explicit client retries then verify that every remaining seat is still sellable.
    /// Every request targets its own distinct seat,
    /// so the only thing that can reject one is running out of capacity under the race — never
    /// a seat collision, which is a separate mechanism already covered by
    /// <see cref="FlightsEndpointsTests.Book_DuplicateSeat_ReturnsConflict"/>.
    /// </summary>
    [Fact]
    public async Task ConcurrentBookings_NeverOversell_AndRemainingCapacityCanBeSold()
    {
        const int seatCapacity = 6;
        const int attempts = 12;

        var client = factory.CreateClient();
        var origin = await TestDataBuilder.CreateAirportAsync(client);
        var destination = await TestDataBuilder.CreateAirportAsync(client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(client, seatCapacity, maxPayloadKg: 100_000m);
        var flight = await TestDataBuilder.CreateFlightAsync(client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddDays(7));

        var passengers = await Task.WhenAll(Enumerable.Range(0, attempts).Select(_ => TestDataBuilder.CreatePassengerAsync(client)));

        var responses = await Task.WhenAll(Enumerable.Range(0, attempts).Select(async i =>
        {
            // Each attempt gets its own HttpClient/connection, mirroring independent callers.
            using var attemptClient = factory.CreateClient();
            var seat = $"{(i / 6) + 1}{(char)('A' + (i % 6))}";
            var request = new CreateBookingRequest(passengers[i].Id, seat, 5m, 100m);
            return await attemptClient.PostAsJsonAsync($"/api/v1/flights/{flight.Id}/bookings", request);
        }));

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var rejected = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.InRange(succeeded, 1, seatCapacity);
        Assert.Equal(attempts - succeeded, rejected);

        var flightAfter = await (await client.GetAsync($"/api/v1/flights/{flight.Id}")).Content
            .ReadFromJsonAsync<FlightResponse>(TestDataBuilder.JsonOptions);
        Assert.Equal(succeeded, flightAfter!.SeatsBooked);

        for (var i = 0; i < attempts && succeeded < seatCapacity; i++)
        {
            if (responses[i].StatusCode == HttpStatusCode.Created)
            {
                continue;
            }

            var seat = $"{(i / 6) + 1}{(char)('A' + (i % 6))}";
            using var retry = await client.PostAsJsonAsync($"/api/v1/flights/{flight.Id}/bookings",
                new CreateBookingRequest(passengers[i].Id, seat, 5m, 100m));
            Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
            succeeded++;
        }

        Assert.Equal(seatCapacity, succeeded);
        var final = await client.GetFromJsonAsync<FlightResponse>($"/api/v1/flights/{flight.Id}", TestDataBuilder.JsonOptions);
        Assert.Equal(seatCapacity, final!.SeatsBooked);
        var bookings = await client.GetFromJsonAsync<List<BookingResponse>>($"/api/v1/flights/{flight.Id}/bookings", TestDataBuilder.JsonOptions);
        Assert.Equal(seatCapacity, bookings!.Count);
        Assert.Equal(seatCapacity, bookings.Select(b => b.Seat).Distinct().Count());
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
