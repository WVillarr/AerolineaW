using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using AerolineaW.Domain.ValueObjects;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Application.Bookings;

public interface IBookingService
{
    Task<BookingResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BookingResponse> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingResponse>> GetByFlightIdAsync(int flightId, CancellationToken cancellationToken = default);

    Task<BookingResponse> CreateAsync(int flightId, CreateBookingRequest request, CancellationToken cancellationToken = default);

    Task<BookingResponse> CheckInAsync(int id, CancellationToken cancellationToken = default);

    Task<BookingResponse> CancelAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class BookingService(IApplicationDbContext db) : IBookingService
{
    /// <summary>
    /// How many times to start the booking attempt over — fresh reads, fresh entities — after
    /// an optimistic-concurrency conflict on <see cref="Flight.RowVersion"/>. Each conflict
    /// means another booking committed against this same flight between our read and our
    /// write; retrying re-checks capacity against the row as it actually stands now, rather
    /// than assuming our original request was invalid.
    /// </summary>
    private const int MaxAttempts = 3;

    public async Task<BookingResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var booking = await FindAsync(id, cancellationToken);
        return booking.Adapt<BookingResponse>();
    }

    public async Task<BookingResponse> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        var normalized = new BookingReference(reference);
        var booking = await BookingQuery().FirstOrDefaultAsync(b => b.Reference == normalized, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), reference);
        return booking.Adapt<BookingResponse>();
    }

    public async Task<IReadOnlyList<BookingResponse>> GetByFlightIdAsync(int flightId, CancellationToken cancellationToken = default)
    {
        var bookings = await BookingQuery().Where(b => b.FlightId == flightId).ToListAsync(cancellationToken);
        return bookings.Adapt<List<BookingResponse>>();
    }

    public async Task<BookingResponse> CreateAsync(int flightId, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var seat = new SeatNumber(request.Seat);

        for (var attempt = 1; ; attempt++)
        {
            // Re-read everything fresh on every attempt, including after a conflict: retrying
            // against entities left over from a failed SaveChanges would just retry with the
            // same stale RowVersion and fail identically forever.
            var passenger = await db.Passengers.FirstOrDefaultAsync(p => p.Id == request.PassengerId, cancellationToken)
                ?? throw new NotFoundException(nameof(Passenger), request.PassengerId);

            var flight = await db.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Bookings)
                .FirstOrDefaultAsync(f => f.Id == flightId, cancellationToken)
                ?? throw new NotFoundException(nameof(Flight), flightId);

            var booking = flight.Book(passenger, seat, request.BaggageWeightKg, request.Price, DateTime.UtcNow);
            db.Bookings.Add(booking);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return booking.Adapt<BookingResponse>();
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                db.ClearChangeTracking();
            }
        }
    }

    public Task<BookingResponse> CheckInAsync(int id, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, b => b.CheckIn(DateTime.UtcNow), cancellationToken);

    public Task<BookingResponse> CancelAsync(int id, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, b => b.Cancel(), cancellationToken);

    private async Task<BookingResponse> ApplyAsync(int id, Action<Booking> transition, CancellationToken cancellationToken)
    {
        var booking = await FindAsync(id, cancellationToken);
        transition(booking);
        await db.SaveChangesAsync(cancellationToken);
        return booking.Adapt<BookingResponse>();
    }

    private IQueryable<Booking> BookingQuery() =>
        db.Bookings.Include(b => b.Flight).Include(b => b.Passenger).OrderBy(b => b.Id);

    private async Task<Booking> FindAsync(int id, CancellationToken cancellationToken) =>
        await BookingQuery().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), id);
}
