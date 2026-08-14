using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Bookings;
using AerolineaW.Application.Flights;
using AerolineaW.Domain.Enums;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class FlightsEndpointsTests(ApiWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateFlight_ThenBook_FollowsTheHappyPath()
    {
        var origin = await TestDataBuilder.CreateAirportAsync(_client);
        var destination = await TestDataBuilder.CreateAirportAsync(_client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);
        var passenger = await TestDataBuilder.CreatePassengerAsync(_client);

        var flight = await TestDataBuilder.CreateFlightAsync(_client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddDays(3));
        Assert.Equal(FlightStatus.Scheduled, flight.Status);

        var bookingResponse = await _client.PostAsJsonAsync(
            $"/api/v1/flights/{flight.Id}/bookings", new CreateBookingRequest(passenger.Id, "1A", 10m, 199m));
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);

        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(BookingStatus.Confirmed, booking!.Status);

        var byReference = await _client.GetAsync($"/api/v1/bookings/by-reference/{booking.Reference}");
        Assert.Equal(HttpStatusCode.OK, byReference.StatusCode);
    }

    [Fact]
    public async Task Book_DuplicateSeat_ReturnsConflict()
    {
        var origin = await TestDataBuilder.CreateAirportAsync(_client);
        var destination = await TestDataBuilder.CreateAirportAsync(_client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);
        var passengerA = await TestDataBuilder.CreatePassengerAsync(_client);
        var passengerB = await TestDataBuilder.CreatePassengerAsync(_client);
        var flight = await TestDataBuilder.CreateFlightAsync(_client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddDays(3));

        var first = await _client.PostAsJsonAsync(
            $"/api/v1/flights/{flight.Id}/bookings", new CreateBookingRequest(passengerA.Id, "2B", 10m, 199m));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync(
            $"/api/v1/flights/{flight.Id}/bookings", new CreateBookingRequest(passengerB.Id, "2B", 10m, 199m));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateFlight_AircraftAlreadyScheduledOverlapping_ReturnsConflict()
    {
        var origin = await TestDataBuilder.CreateAirportAsync(_client);
        var destination = await TestDataBuilder.CreateAirportAsync(_client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);
        var departure = DateTime.UtcNow.AddDays(5);

        await TestDataBuilder.CreateFlightAsync(_client, aircraft.Id, origin.Id, destination.Id, departure);

        // Starts before the first flight lands plus its 45-minute turnaround buffer.
        var overlappingRequest = new CreateFlightRequest(
            "FLX99", aircraft.Id, origin.Id, destination.Id, departure.AddHours(1), departure.AddHours(3));
        var overlapResponse = await _client.PostAsJsonAsync("/api/v1/flights", overlappingRequest);

        Assert.Equal(HttpStatusCode.Conflict, overlapResponse.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_BoardDepartLand_TransitionsCorrectly()
    {
        var origin = await TestDataBuilder.CreateAirportAsync(_client);
        var destination = await TestDataBuilder.CreateAirportAsync(_client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);
        var flight = await TestDataBuilder.CreateFlightAsync(_client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddMinutes(5));

        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/flights/{flight.Id}/board", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/flights/{flight.Id}/depart", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/flights/{flight.Id}/land", null)).StatusCode);

        var landed = await (await _client.GetAsync($"/api/v1/flights/{flight.Id}")).Content.ReadFromJsonAsync<FlightResponse>();
        Assert.Equal(FlightStatus.Landed, landed!.Status);
    }
}
