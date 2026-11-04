using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Bookings;
using AerolineaW.Application.Common;
using AerolineaW.Application.Flights;
using AerolineaW.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class ReleaseRegressionTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task CancelThenRebookSameSeat_PreservesHistoryAndCounters()
    {
        using var client = factory.CreateClient();
        var flightId = await CreateFlightAsync(client);
        var passenger = await TestDataBuilder.CreatePassengerAsync(client);
        var request = new CreateBookingRequest(passenger.Id, "1A", 10m, 100m);
        var first = await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/bookings", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var original = (await first.Content.ReadFromJsonAsync<BookingResponse>(TestDataBuilder.JsonOptions))!;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/bookings/{original.Id}/cancel", null)).StatusCode);

        var second = await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/bookings", request);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var replacement = (await second.Content.ReadFromJsonAsync<BookingResponse>(TestDataBuilder.JsonOptions))!;
        Assert.NotEqual(original.Id, replacement.Id);
        Assert.NotEqual(original.Reference, replacement.Reference);
        var history = (await client.GetFromJsonAsync<List<BookingResponse>>($"/api/v1/flights/{flightId}/bookings", TestDataBuilder.JsonOptions))!;
        Assert.Equal(2, history.Count);
        Assert.Equal(BookingStatus.Cancelled, history.Single(b => b.Id == original.Id).Status);
        Assert.Equal(BookingStatus.Confirmed, history.Single(b => b.Id == replacement.Id).Status);
        await using var db = factory.CreateDbContext();
        var persisted = await db.Flights.SingleAsync(f => f.Id == flightId);
        Assert.Equal(1, persisted.SeatsBooked);
        Assert.Equal(10m, persisted.PayloadBookedKg);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelAfterDeparture_ReturnsConflictAndPreservesCounters(bool landed)
    {
        using var client = factory.CreateClient();
        var flightId = await CreateFlightAsync(client);
        var passenger = await TestDataBuilder.CreatePassengerAsync(client);
        var created = await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/bookings", new CreateBookingRequest(passenger.Id, "1A", 10m, 100m));
        var booking = (await created.Content.ReadFromJsonAsync<BookingResponse>(TestDataBuilder.JsonOptions))!;
        (await client.PostAsync($"/api/v1/flights/{flightId}/board", null)).EnsureSuccessStatusCode();
        var departedAt = DateTime.UtcNow;
        (await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/depart", new RecordActualTimeRequest(departedAt))).EnsureSuccessStatusCode();
        if (landed)
        {
            (await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/land", new RecordActualTimeRequest(departedAt.AddHours(1)))).EnsureSuccessStatusCode();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/v1/bookings/{booking.Id}/cancel", null)).StatusCode);
        await using var db = factory.CreateDbContext();
        Assert.Equal(BookingStatus.Confirmed, (await db.Bookings.SingleAsync(b => b.Id == booking.Id)).Status);
        var flight = await db.Flights.SingleAsync(f => f.Id == flightId);
        Assert.Equal(1, flight.SeatsBooked);
        Assert.Equal(10m, flight.PayloadBookedKg);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleCheckIn_CannotOverwriteCancellation(bool cancelFlight)
    {
        using var client = factory.CreateClient();
        var flightId = await CreateFlightAsync(client);
        var passenger = await TestDataBuilder.CreatePassengerAsync(client);
        var created = await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/bookings", new CreateBookingRequest(passenger.Id, "1A", 10m, 100m));
        var booking = (await created.Content.ReadFromJsonAsync<BookingResponse>(TestDataBuilder.JsonOptions))!;
        await using var stale = factory.CreateDbContext();
        var pending = await stale.Bookings.Include(b => b.Flight).SingleAsync(b => b.Id == booking.Id);
        pending.CheckIn(DateTime.UtcNow);

        var path = cancelFlight ? $"/api/v1/flights/{flightId}/cancel" : $"/api/v1/bookings/{booking.Id}/cancel";
        (await client.PostAsync(path, null)).EnsureSuccessStatusCode();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        await using var fresh = factory.CreateDbContext();
        Assert.Equal(BookingStatus.Cancelled, (await fresh.Bookings.SingleAsync(b => b.Id == booking.Id)).Status);
    }

    [Theory]
    [InlineData(9, HttpStatusCode.Conflict)]
    [InlineData(10, HttpStatusCode.OK)]
    public async Task AssignAircraft_RespectsBookedPayload(int payload, HttpStatusCode expected)
    {
        using var client = factory.CreateClient();
        var flightId = await CreateFlightAsync(client);
        var passenger = await TestDataBuilder.CreatePassengerAsync(client);
        (await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/bookings", new CreateBookingRequest(passenger.Id, "1A", 10m, 100m))).EnsureSuccessStatusCode();
        var before = (await client.GetFromJsonAsync<FlightResponse>($"/api/v1/flights/{flightId}", TestDataBuilder.JsonOptions))!;
        var replacement = await TestDataBuilder.CreateAircraftAsync(client, maxPayloadKg: payload);

        var response = await client.PutAsJsonAsync($"/api/v1/flights/{flightId}/aircraft", new AssignAircraftRequest(replacement.Id));

        Assert.Equal(expected, response.StatusCode);
        var after = (await client.GetFromJsonAsync<FlightResponse>($"/api/v1/flights/{flightId}", TestDataBuilder.JsonOptions))!;
        Assert.Equal(expected == HttpStatusCode.OK ? replacement.Id : before.AircraftId, after.AircraftId);
        Assert.Equal(1, after.SeatsBooked);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Land_NotAfterDeparture_ReturnsConflictWithoutMutation(int minutes)
    {
        using var client = factory.CreateClient();
        var flightId = await CreateFlightAsync(client);
        (await client.PostAsync($"/api/v1/flights/{flightId}/board", null)).EnsureSuccessStatusCode();
        var departedAt = DateTime.UtcNow;
        (await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/depart", new RecordActualTimeRequest(departedAt))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/flights/{flightId}/land", new RecordActualTimeRequest(departedAt.AddMinutes(minutes)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var after = (await client.GetFromJsonAsync<FlightResponse>($"/api/v1/flights/{flightId}", TestDataBuilder.JsonOptions))!;
        Assert.Equal(FlightStatus.InFlight, after.Status);
        Assert.Null(after.ActualArrivalUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentScheduling_NeverPersistsOverlappingFlights(bool reassign)
    {
        using var client = factory.CreateClient();
        var origin = await TestDataBuilder.CreateAirportAsync(client);
        var destination = await TestDataBuilder.CreateAirportAsync(client);
        var target = await TestDataBuilder.CreateAircraftAsync(client);
        var departure = DateTime.UtcNow.AddDays(5);
        var request = new CreateFlightRequest("RACE", target.Id, origin.Id, destination.Id, departure, departure.AddHours(2));
        var ids = new List<int>();
        if (reassign)
        {
            for (var i = 0; i < 2; i++)
            {
                var aircraft = await TestDataBuilder.CreateAircraftAsync(client);
                var flight = await TestDataBuilder.CreateFlightAsync(client, aircraft.Id, origin.Id, destination.Id, departure);
                ids.Add(flight.Id);
            }
        }

        var barrier = new SaveBarrier();
        await using var first = factory.CreateDbContext(barrier);
        await using var second = factory.CreateDbContext(barrier);
        var contexts = new[] { first, second };
        var outcomes = await Task.WhenAll(contexts.Select(async (db, i) =>
        {
            var service = new FlightService(db);
            try
            {
                if (reassign)
                {
                    await service.AssignAircraftAsync(ids[i], new AssignAircraftRequest(target.Id));
                }
                else
                {
                    await service.CreateAsync(request);
                }

                return true;
            }
            catch (Domain.Exceptions.AircraftScheduleConflictException)
            {
                return false;
            }
        }));

        Assert.Single(outcomes, succeeded => succeeded);
        await using var fresh = factory.CreateDbContext();
        Assert.Equal(1, await fresh.Flights.CountAsync(f => f.AircraftId == target.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentBookings_ProtectSameSeatAndLastSeat(bool sameSeat)
    {
        using var client = factory.CreateClient();
        var origin = await TestDataBuilder.CreateAirportAsync(client);
        var destination = await TestDataBuilder.CreateAirportAsync(client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(client, seatCapacity: sameSeat ? 2 : 1);
        var flightId = (await TestDataBuilder.CreateFlightAsync(client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddHours(12))).Id;
        var passenger = await TestDataBuilder.CreatePassengerAsync(client);
        var barrier = new SaveBarrier();
        await using var first = factory.CreateDbContext(barrier);
        await using var second = factory.CreateDbContext(barrier);
        var outcomes = await Task.WhenAll(new[] { first, second }.Select(async (db, i) =>
        {
            try
            {
                var request = new CreateBookingRequest(passenger.Id, sameSeat || i == 0 ? "1A" : "1B", 10m, 100m);
                await new BookingService(db).CreateAsync(flightId, request);
                return true;
            }
            catch (Domain.Exceptions.SeatAlreadyTakenException)
            {
                return false;
            }
            catch (Domain.Exceptions.FlightFullException)
            {
                return false;
            }
            catch (PersistenceConflictException)
            {
                return false;
            }
        }));

        Assert.Single(outcomes, succeeded => succeeded);
        await using var fresh = factory.CreateDbContext();
        Assert.Equal(1, await fresh.Bookings.CountAsync(b => b.FlightId == flightId));
        Assert.Equal(1, (await fresh.Flights.SingleAsync(f => f.Id == flightId)).SeatsBooked);
    }

    private static async Task<int> CreateFlightAsync(HttpClient client)
    {
        var origin = await TestDataBuilder.CreateAirportAsync(client);
        var destination = await TestDataBuilder.CreateAirportAsync(client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(client);
        return (await TestDataBuilder.CreateFlightAsync(client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddHours(12))).Id;
    }
}
