using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Infrastructure.Persistence;

public sealed class AirlineDbContext(DbContextOptions<AirlineDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Airport> Airports => Set<Airport>();

    public DbSet<Aircraft> Aircraft => Set<Aircraft>();

    public DbSet<Flight> Flights => Set<Flight>();

    public DbSet<Passenger> Passengers => Set<Passenger>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public void ClearChangeTracking() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AirlineDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
