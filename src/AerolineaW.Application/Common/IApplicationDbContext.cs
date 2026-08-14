using AerolineaW.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Application.Common;

/// <summary>
/// The persistence contract application services depend on. Defined here rather than in
/// Infrastructure so that Application never references EF Core's SQL Server provider — only
/// the provider-agnostic <c>Microsoft.EntityFrameworkCore</c> package, which is enough to
/// express <see cref="DbSet{TEntity}"/> and <see cref="SaveChangesAsync"/>. Infrastructure's
/// <c>AirlineDbContext</c> implements this.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Airport> Airports { get; }

    // Fully qualified: within Application, the bare name "Aircraft" resolves to the sibling
    // AerolineaW.Application.Aircraft namespace before it ever reaches this using-imported type.
    DbSet<AerolineaW.Domain.Entities.Aircraft> Aircraft { get; }

    DbSet<Flight> Flights { get; }

    DbSet<Passenger> Passengers { get; }

    DbSet<Booking> Bookings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches every tracked entity. Used by <c>BookingService</c> to discard a failed
    /// booking attempt after an optimistic-concurrency conflict, so the next retry re-reads
    /// its entities fresh instead of resubmitting the same stale <c>RowVersion</c>.
    /// </summary>
    void ClearChangeTracking();
}
