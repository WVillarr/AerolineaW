using System.Data;
using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using Microsoft.Data.SqlClient;
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

    public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A retry must repeat the reads, not reuse entities from a rolled-back attempt.
            ChangeTracker.Clear();
            await using var transaction = await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 or 547 })
        {
            throw new PersistenceConflictException(exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AirlineDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
