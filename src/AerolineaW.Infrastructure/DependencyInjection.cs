using AerolineaW.Application.Common;
using AerolineaW.Infrastructure.Persistence;
using AerolineaW.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AerolineaW.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("AirlineDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'AirlineDatabase' is not configured.");
        }

        services.AddDbContext<AirlineDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AirlineDbContext>());

        // Development only: never runs against a real environment's database.
        if (environment.IsDevelopment())
        {
            services.AddHostedService<DemoDataSeeder>();
        }

        return services;
    }
}
