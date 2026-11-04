using System.Net;
using System.Net.Http.Json;
using AerolineaW.Api.ErrorHandling;
using AerolineaW.Application.Airports;
using AerolineaW.Application.Common;
using AerolineaW.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

public sealed class ErrorHandlerTests
{
    [Theory]
    [InlineData("unexpected", 500)]
    [InlineData("constraint", 409)]
    [InlineData("concurrency", 409)]
    public async Task PersistenceErrors_ReturnSafeProblemDetails(string kind, int expectedStatus)
    {
        const string privateDetail = "PRIVATE_DATABASE_DETAIL";
        Exception exception = kind switch
        {
            "constraint" => new PersistenceConflictException(new InvalidOperationException(privateDetail)),
            "concurrency" => new DbUpdateConcurrencyException(privateDetail),
            _ => new DbUpdateException(privateDetail),
        };
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType, StringComparison.Ordinal);
        body.Position = 0;
        using var reader = new StreamReader(body);
        var json = await reader.ReadToEndAsync();
        Assert.DoesNotContain(privateDetail, json, StringComparison.Ordinal);
        Assert.Contains($"\"status\":{expectedStatus}", json, StringComparison.Ordinal);
    }
}

[Collection(nameof(SharedApiFixture))]
public sealed class PersistenceErrorEndpointTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task UnexpectedSaveFailure_ReturnsGeneric500()
    {
        using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<AirlineDbContext>(options => options.AddInterceptors(new FailingSaveInterceptor()))));
        using var client = failingFactory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/airports", new CreateAirportRequest("ERR", "Airport", "City", "Country", "America/New_York"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var details = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal("An unexpected error occurred.", details.Detail);
        Assert.DoesNotContain("PRIVATE", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("PRIVATE_DATABASE_DETAIL");
    }
}
