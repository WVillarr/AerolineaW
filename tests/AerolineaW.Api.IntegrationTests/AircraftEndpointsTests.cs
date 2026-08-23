using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Aircraft;
using AerolineaW.Domain.Enums;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class AircraftEndpointsTests(ApiWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task StatusLifecycle_TransitionsTheAircraft()
    {
        var aircraft = await TestDataBuilder.CreateAircraftAsync(_client);

        var maintenance = await _client.PostAsync($"/api/v1/aircraft/{aircraft.Id}/send-to-maintenance", null);
        var returned = await _client.PostAsync($"/api/v1/aircraft/{aircraft.Id}/return-to-service", null);
        var retired = await _client.PostAsync($"/api/v1/aircraft/{aircraft.Id}/retire", null);

        Assert.Equal(HttpStatusCode.OK, maintenance.StatusCode);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retired.StatusCode);
        var final = await (await _client.GetAsync($"/api/v1/aircraft/{aircraft.Id}"))
            .Content.ReadFromJsonAsync<AircraftResponse>(TestDataBuilder.JsonOptions);
        Assert.Equal(AircraftStatus.Retired, final!.Status);
    }

    [Fact]
    public async Task Create_DuplicateRegistration_ReturnsConflict()
    {
        var created = await TestDataBuilder.CreateAircraftAsync(_client);
        var request = new CreateAircraftRequest(created.Registration, "Another Model", 100, 10_000m);

        var response = await _client.PostAsJsonAsync("/api/v1/aircraft", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
