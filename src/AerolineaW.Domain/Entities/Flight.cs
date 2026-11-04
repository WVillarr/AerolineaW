using AerolineaW.Domain.Enums;
using AerolineaW.Domain.Exceptions;
using AerolineaW.Domain.ValueObjects;

namespace AerolineaW.Domain.Entities;

/// <summary>
/// A scheduled service between two airports on a specific aircraft. This is the aggregate
/// root for booking: every <see cref="Booking"/> is created through <see cref="Book"/>, which
/// is where capacity, payload, seat-uniqueness, and the booking-window rules all live.
/// </summary>
public sealed class Flight
{
    /// <summary>Minimum ground time an aircraft needs between two flights it operates.</summary>
    public static readonly TimeSpan TurnaroundBuffer = TimeSpan.FromMinutes(45);

    /// <summary>Bookings close this long before scheduled departure.</summary>
    public static readonly TimeSpan BookingCutoff = TimeSpan.FromMinutes(45);

    /// <summary>Check-in opens this long before scheduled departure.</summary>
    public static readonly TimeSpan CheckInOpens = TimeSpan.FromHours(24);

    /// <summary>Check-in closes this long before scheduled departure.</summary>
    public static readonly TimeSpan CheckInCutoff = TimeSpan.FromMinutes(45);

    private readonly List<Booking> _bookings = [];

    public int Id { get; private set; }

    public string FlightNumber { get; private set; } = string.Empty;

    public int AircraftId { get; private set; }

    public Aircraft Aircraft { get; private set; } = null!;

    public int OriginAirportId { get; private set; }

    public Airport OriginAirport { get; private set; } = null!;

    public int DestinationAirportId { get; private set; }

    public Airport DestinationAirport { get; private set; } = null!;

    public DateTime ScheduledDepartureUtc { get; private set; }

    public DateTime ScheduledArrivalUtc { get; private set; }

    public DateTime? ActualDepartureUtc { get; private set; }

    public DateTime? ActualArrivalUtc { get; private set; }

    public FlightStatus Status { get; private set; }

    /// <summary>
    /// Seats sold so far. Stored and incremented on the flight itself — rather than derived
    /// from counting <see cref="Bookings"/> at read time — so that <see cref="Book"/> always
    /// issues an UPDATE against this row. That is what makes the <see cref="RowVersion"/>
    /// concurrency token actually guard capacity: two concurrent bookings for different seats
    /// both increment this same column, so the second SaveChanges to commit sees a stale
    /// RowVersion and fails instead of silently overselling the flight.
    /// </summary>
    public int SeatsBooked { get; private set; }

    public decimal PayloadBookedKg { get; private set; }

    /// <summary>EF Core concurrency token, configured as a rowversion column in Infrastructure.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private Flight()
    {
        // Reserved for EF Core materialization.
    }

    public Flight(
        string flightNumber,
        Aircraft aircraft,
        Airport origin,
        Airport destination,
        DateTime scheduledDepartureUtc,
        DateTime scheduledArrivalUtc)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            throw new ArgumentException("Flight number cannot be empty.", nameof(flightNumber));
        }

        // Compared by IATA code, not Id: two freshly-constructed (unsaved, Id == 0) Airport
        // instances must not look like "the same airport" just because neither has a key yet.
        if (origin.Code == destination.Code)
        {
            throw new InvalidFlightRouteException(origin.Code.Value);
        }

        if (scheduledArrivalUtc <= scheduledDepartureUtc)
        {
            throw new InvalidFlightScheduleException();
        }

        EnsureAircraftAvailable(aircraft);

        FlightNumber = flightNumber.Trim().ToUpperInvariant();
        AircraftId = aircraft.Id;
        Aircraft = aircraft;
        OriginAirportId = origin.Id;
        OriginAirport = origin;
        DestinationAirportId = destination.Id;
        DestinationAirport = destination;
        ScheduledDepartureUtc = scheduledDepartureUtc;
        ScheduledArrivalUtc = scheduledArrivalUtc;
        Status = FlightStatus.Scheduled;
    }

    /// <summary>
    /// Whether this flight's ground time (including <see cref="TurnaroundBuffer"/> on both
    /// ends) overlaps a candidate schedule. Application code uses this to check a new or
    /// updated flight against every other flight already assigned to the same aircraft,
    /// since answering that question requires a database query the domain model can't do.
    /// </summary>
    public bool OverlapsWithTurnaround(DateTime otherDepartureUtc, DateTime otherArrivalUtc)
    {
        var start = ScheduledDepartureUtc - TurnaroundBuffer;
        var end = ScheduledArrivalUtc + TurnaroundBuffer;
        return start < otherArrivalUtc && otherDepartureUtc < end;
    }

    /// <summary>Reassigns the aircraft operating this flight (e.g. after a maintenance swap).</summary>
    public void AssignAircraft(Aircraft aircraft)
    {
        ArgumentNullException.ThrowIfNull(aircraft);

        if (Status is FlightStatus.InFlight or FlightStatus.Landed)
        {
            throw new InvalidFlightTransitionException($"Cannot reassign aircraft on flight {FlightNumber}: status is {Status}.");
        }

        EnsureAircraftAvailable(aircraft);

        if (aircraft.SeatCapacity < SeatsBooked)
        {
            throw new FlightFullException(FlightNumber);
        }

        if (aircraft.MaxPayloadKg < PayloadBookedKg)
        {
            throw new PayloadExceededException(FlightNumber);
        }

        AircraftId = aircraft.Id;
        Aircraft = aircraft;
    }

    public Booking Book(Passenger passenger, SeatNumber seat, decimal baggageWeightKg, decimal price, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(passenger);
        ArgumentNullException.ThrowIfNull(seat);

        if (Status != FlightStatus.Scheduled)
        {
            throw new InvalidFlightTransitionException($"Cannot book flight {FlightNumber}: status is {Status}.");
        }

        if (nowUtc > ScheduledDepartureUtc - BookingCutoff)
        {
            throw new BookingWindowClosedException(FlightNumber);
        }

        if (SeatsBooked >= Aircraft.SeatCapacity)
        {
            throw new FlightFullException(FlightNumber);
        }

        if (_bookings.Any(b => b.Status != BookingStatus.Cancelled && b.Seat == seat))
        {
            throw new SeatAlreadyTakenException(FlightNumber, seat.Value);
        }

        if (PayloadBookedKg + baggageWeightKg > Aircraft.MaxPayloadKg)
        {
            throw new PayloadExceededException(FlightNumber);
        }

        var booking = new Booking(this, passenger, seat, baggageWeightKg, price, nowUtc);
        _bookings.Add(booking);
        SeatsBooked++;
        PayloadBookedKg += baggageWeightKg;
        return booking;
    }

    /// <summary>Called by <see cref="Booking.Cancel"/> to give the seat and payload back.</summary>
    internal void ReleaseSeat(decimal baggageWeightKg)
    {
        SeatsBooked = Math.Max(0, SeatsBooked - 1);
        PayloadBookedKg = Math.Max(0, PayloadBookedKg - baggageWeightKg);
    }

    public void Board() => TransitionTo(FlightStatus.Scheduled, FlightStatus.Boarding);

    public void Depart(DateTime actualDepartureUtc)
    {
        TransitionTo(FlightStatus.Boarding, FlightStatus.InFlight);
        ActualDepartureUtc = actualDepartureUtc;
    }

    public void Land(DateTime actualArrivalUtc)
    {
        if (ActualDepartureUtc.HasValue && actualArrivalUtc <= ActualDepartureUtc.Value)
        {
            throw new InvalidFlightTransitionException("Actual arrival must be after actual departure.");
        }

        TransitionTo(FlightStatus.InFlight, FlightStatus.Landed);
        ActualArrivalUtc = actualArrivalUtc;
    }

    public void Delay()
    {
        if (Status is not (FlightStatus.Scheduled or FlightStatus.Boarding))
        {
            throw new InvalidFlightTransitionException($"Cannot delay flight {FlightNumber} from status {Status}.");
        }

        Status = FlightStatus.Delayed;
    }

    public void ResumeFromDelay() => TransitionTo(FlightStatus.Delayed, FlightStatus.Scheduled);

    public void Cancel()
    {
        if (Status is FlightStatus.InFlight or FlightStatus.Landed or FlightStatus.Cancelled)
        {
            throw new InvalidFlightTransitionException($"Cannot cancel flight {FlightNumber} from status {Status}.");
        }

        foreach (var booking in _bookings.Where(b => b.Status != BookingStatus.Cancelled))
        {
            booking.CancelDueToFlightCancellation();
        }

        Status = FlightStatus.Cancelled;
    }

    private static void EnsureAircraftAvailable(Aircraft aircraft)
    {
        if (aircraft.Status != AircraftStatus.InService)
        {
            throw new AircraftUnavailableException(aircraft.Registration, aircraft.Status);
        }
    }

    private void TransitionTo(FlightStatus expectedCurrent, FlightStatus target)
    {
        if (Status != expectedCurrent)
        {
            throw new InvalidFlightTransitionException(
                $"Cannot move flight {FlightNumber} to {target} from {Status}; expected {expectedCurrent}.");
        }

        Status = target;
    }
}
