using AerolineaW.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

/// <summary>
/// Boots the real API (<c>Program</c>, unmodified) against a real, disposable SQL Server
/// running in a Testcontainers container — not EF Core InMemory, which doesn't enforce unique
/// indexes or a rowversion concurrency token and would make the whole concurrency story a lie.
/// Runs with ASPNETCORE_ENVIRONMENT=Testing, so DemoDataSeeder (Development-only) never runs:
/// every test arranges its own fixture data through <see cref="TestDataBuilder"/> instead of
/// depending on shared seed data.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    // Explicit interface implementations: WebApplicationFactory<T> already has its own
    // ValueTask DisposeAsync() (from IAsyncDisposable), a different signature from
    // IAsyncLifetime's Task DisposeAsync() — implementing it explicitly is what lets both
    // coexist without one hiding or trying (and failing) to override the other.
    async Task IAsyncLifetime.InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        // Touching Services is what actually builds the host, which is why the container has
        // to be running (and ConfigureWebHost's connection string ready) before this line.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("ConnectionStrings:AirlineDatabase", _sqlContainer.GetConnectionString()),
        ]));
    }
}
