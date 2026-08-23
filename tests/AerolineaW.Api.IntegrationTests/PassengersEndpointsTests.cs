using System.Net;
using System.Net.Http.Json;
using AerolineaW.Application.Passengers;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

[Collection(nameof(SharedApiFixture))]
public sealed class PassengersEndpointsTests(ApiWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateUpdateAndDelete_CompletesPassengerLifecycle()
    {
        var passenger = await TestDataBuilder.CreatePassengerAsync(_client);

        var update = await _client.PutAsJsonAsync(
            $"/api/v1/passengers/{passenger.Id}/contact",
            new UpdatePassengerContactRequest("Updated Country", "+1 555 0199"));
        var delete = await _client.DeleteAsync($"/api/v1/passengers/{passenger.Id}");
        var get = await _client.GetAsync($"/api/v1/passengers/{passenger.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
