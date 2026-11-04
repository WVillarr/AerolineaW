using AerolineaW.Domain.Enums;
using AerolineaW.Domain.Exceptions;
using AerolineaW.Domain.ValueObjects;

namespace AerolineaW.Domain.Entities;

/// <summary>
/// A single passenger's reservation on a <see cref="Flight"/>.
/// </summary>
/// <remarks>
/// Booking is only ever created through <see cref="Flight.Book"/>, which is where every
/// creation-time invariant (capacity, payload, seat uniqueness, booking window) is enforced —
/// the constructor below is <c>internal</c> specifically to force that path. After creation,
/// though, Booking is queried and mutated directly rather than always routed back through its
/// parent Flight: a real booking system needs to look a reservation up by its reference alone
/// (a passenger presenting a PNR at check-in has no reason to know which flight it's on), and
/// requiring every check-in or cancellation to load the whole Flight aggregate — bookings and
/// all — would be ceremony this project's scope doesn't justify. That is a narrower aggregate
/// boundary than a textbook DDD implementation would use, and a deliberate one.
/// </remarks>
public sealed class Booking
{
    public int Id { get; private set; }

    public BookingReference Reference { get; private set; } = null!;

    public int FlightId { get; private set; }

    public Flight Flight { get; private set; } = null!;

    public int PassengerId { get; private set; }

    public Passenger Passenger { get; private set; } = null!;

    public SeatNumber Seat { get; private set; } = null!;

    public decimal BaggageWeightKg { get; private set; }

    public decimal Price { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTime BookedAtUtc { get; private set; }

    public DateTime? CheckedInAtUtc { get; private set; }

    /// <summary>Guards against concurrent check-in and cancellation overwriting each other.</summary>
    public byte[] RowVersion { get; private set; } = [];

    private Booking()
    {
        // Reserved for EF Core materialization.
    }

    internal Booking(Flight flight, Passenger passenger, SeatNumber seat, decimal baggageWeightKg, decimal price, DateTime bookedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(flight);
        ArgumentNullException.ThrowIfNull(passenger);
        ArgumentNullException.ThrowIfNull(seat);

        if (baggageWeightKg < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baggageWeightKg), baggageWeightKg, "Baggage weight cannot be negative.");
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
        }

        Reference = BookingReference.Generate();
        Flight = flight;
        FlightId = flight.Id;
        Passenger = passenger;
        PassengerId = passenger.Id;
        Seat = seat;
        BaggageWeightKg = baggageWeightKg;
        Price = price;
        Status = BookingStatus.Confirmed;
        BookedAtUtc = bookedAtUtc;
    }

    public void CheckIn(DateTime nowUtc)
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidBookingStatusException($"Booking {Reference} cannot check in from status {Status}.");
        }

        var departureUtc = Flight.ScheduledDepartureUtc;
        if (nowUtc < departureUtc - Flight.CheckInOpens || nowUtc > departureUtc - Flight.CheckInCutoff)
        {
            throw new CheckInWindowException(Reference.Value);
        }

        Status = BookingStatus.CheckedIn;
        CheckedInAtUtc = nowUtc;
    }

    /// <summary>Passenger-initiated cancellation: releases the seat back to the flight.</summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new InvalidBookingStatusException($"Booking {Reference} is already cancelled.");
        }

        if (Flight.Status is not (FlightStatus.Scheduled or FlightStatus.Delayed or FlightStatus.Boarding))
        {
            throw new InvalidBookingStatusException($"Booking {Reference} cannot be cancelled after departure.");
        }

        Flight.ReleaseSeat(BaggageWeightKg);
        Status = BookingStatus.Cancelled;
    }

    /// <summary>
    /// Called by <see cref="Flight.Cancel"/> when the whole flight is cancelled. Unlike
    /// <see cref="Cancel"/>, this does not release the seat back on the flight: the flight
    /// itself is being taken out of service, so its seat/payload counters no longer matter,
    /// and preserving them lets later reporting show how full the flight was when it was cancelled.
    /// </summary>
    internal void CancelDueToFlightCancellation()
    {
        Status = BookingStatus.Cancelled;
    }
}
