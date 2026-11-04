using AerolineaW.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace AerolineaW.Api.IntegrationTests;

/// <summary>
/// Boots the real API (<c>Program</c>, unmodified) against a real, disposable SQL Server
/// running in Testcontainers by default (or an explicitly configured disposable SQL Server
/// database) — not EF Core InMemory, which doesn't enforce unique
/// indexes or a rowversion concurrency token and would make the whole concurrency story a lie.
/// Runs with ASPNETCORE_ENVIRONMENT=Testing, so DemoDataSeeder (Development-only) never runs:
/// every test arranges its own fixture data through <see cref="TestDataBuilder"/> instead of
/// depending on shared seed data.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;

    internal string ConnectionString { get; private set; } = string.Empty;

    internal AirlineDbContext CreateDbContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<AirlineDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.EnableRetryOnFailure())
            .AddInterceptors(interceptors)
            .Options);

    // Explicit interface implementations: WebApplicationFactory<T> already has its own
    // ValueTask DisposeAsync() (from IAsyncDisposable), a different signature from
    // IAsyncLifetime's Task DisposeAsync() — implementing it explicitly is what lets both
    // coexist without one hiding or trying (and failing) to override the other.
    async Task IAsyncLifetime.InitializeAsync()
    {
        var externalServer = Environment.GetEnvironmentVariable("AEROLINEAW_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(externalServer))
        {
            _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _sqlContainer.StartAsync();
            ConnectionString = _sqlContainer.GetConnectionString();
        }
        else
        {
            // Never migrate or delete the supplied database: use only its server/credentials.
            ConnectionString = new SqlConnectionStringBuilder(externalServer)
            {
                InitialCatalog = $"AerolineaW_Tests_{Guid.NewGuid():N}",
            }.ConnectionString;
        }

        // Touching Services is what actually builds the host, which is why the container has
        // to be running (and ConfigureWebHost's connection string ready) before this line.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            if (_sqlContainer is not null)
            {
                await _sqlContainer.DisposeAsync();
            }
            else if (!string.IsNullOrEmpty(ConnectionString))
            {
                await using var db = CreateDbContext();
                await db.Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            await base.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AirlineDatabase", ConnectionString);
    }
}
