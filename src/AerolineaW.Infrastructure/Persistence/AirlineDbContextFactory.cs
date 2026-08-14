using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AerolineaW.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> run directly against this project, without a fully
/// configured host: <c>dotnet ef</c> falls back to this whenever it can't resolve
/// <see cref="DbContextOptions{TContext}"/> through the API project's DI container. The
/// connection string below is never actually opened — design-time tooling only needs to know
/// which SQL dialect to generate migrations for.
/// </summary>
public sealed class AirlineDbContextFactory : IDesignTimeDbContextFactory<AirlineDbContext>
{
    public AirlineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AirlineDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=AerolineaW;Trusted_Connection=True;TrustServerCertificate=True");

        return new AirlineDbContext(optionsBuilder.Options);
    }
}
