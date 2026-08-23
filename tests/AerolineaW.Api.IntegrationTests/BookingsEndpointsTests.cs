using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Bookings;
using AerolineaW.Domain.Enums;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class BookingsEndpointsTests(ApiWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CheckInThenCancel_CompletesBookingLifecycle()
    {
        var origin = await TestDataBuilder.CreateAirportAsync(_client);
        var destination = await TestDataBuilder.CreateAirportAsync(_client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);
        var passenger = await TestDataBuilder.CreatePassengerAsync(_client);
        var flight = await TestDataBuilder.CreateFlightAsync(
            _client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddHours(12));
        var create = await _client.PostAsJsonAsync(
            $"/api/v1/flights/{flight.Id}/bookings", new CreateBookingRequest(passenger.Id, "4D", 10m, 199m));
        var booking = await create.Content.ReadFromJsonAsync<BookingResponse>(TestDataBuilder.JsonOptions);

        var checkIn = await _client.PostAsync($"/api/v1/bookings/{booking!.Id}/check-in", null);
        var cancel = await _client.PostAsync($"/api/v1/bookings/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var final = await (await _client.GetAsync($"/api/v1/bookings/{booking.Id}"))
            .Content.ReadFromJsonAsync<BookingResponse>(TestDataBuilder.JsonOptions);
        Assert.Equal(BookingStatus.Cancelled, final!.Status);
    }
}
