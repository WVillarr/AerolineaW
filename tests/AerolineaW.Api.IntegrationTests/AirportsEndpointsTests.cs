using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Airports;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class AirportsEndpointsTests(ApiWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateThenGet_RoundTripsTheAirport()
    {
        var created = await TestDataBuilder.CreateAirportAsync(_client);

        var getResponse = await _client.GetAsync($"/api/v1/airports/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<AirportResponse>(TestDataBuilder.JsonOptions);
        Assert.Equal(created.Code, fetched!.Code);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/airports/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidCode_ReturnsBadRequest()
    {
        var request = new CreateAirportRequest("TOOLONG", "Name", "City", "Country", "America/New_York");

        var response = await _client.PostAsJsonAsync("/api/v1/airports", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var created = await TestDataBuilder.CreateAirportAsync(_client);
        var request = new CreateAirportRequest(created.Code, "Another Name", "Another City", "Country", "America/New_York");

        var response = await _client.PostAsJsonAsync("/api/v1/airports", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReferencedAirport_ReturnsConflict()
    {
        var origin = await TestDataBuilder.CreateAirportAsync(_client);
        var destination = await TestDataBuilder.CreateAirportAsync(_client);
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);
        await TestDataBuilder.CreateFlightAsync(_client, aircraft.Id, origin.Id, destination.Id, DateTime.UtcNow.AddDays(4));

        var response = await _client.DeleteAsync($"/api/v1/airports/{origin.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
