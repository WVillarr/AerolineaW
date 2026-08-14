using AerolineaW.Domain.Common;
using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;

namespace AerolineaW.Domain.Exceptions;

/// <summary>The flight has no more seats to sell.</summary>
public sealed class FlightFullException(string flightNumber)
    : DomainException($"Flight {flightNumber} is fully booked.")
{
}

/// <summary>The requested seat on this flight is already held by another active booking.</summary>
public sealed class SeatAlreadyTakenException(string flightNumber, string seat)
    : DomainException($"Seat {seat} on flight {flightNumber} is already taken.")
{
}

/// <summary>The flight cannot move to the requested status from its current one.</summary>
public sealed class InvalidFlightTransitionException(string message)
    : DomainException(message)
{
}

/// <summary>Accepting this baggage would exceed the aircraft's maximum payload.</summary>
public sealed class PayloadExceededException(string flightNumber)
    : DomainException($"Flight {flightNumber} cannot accept more baggage weight: maximum payload reached.")
{
}

/// <summary>Bookings close a fixed window before scheduled departure.</summary>
public sealed class BookingWindowClosedException(string flightNumber)
    : DomainException($"Bookings for flight {flightNumber} are closed.")
{
}

/// <summary>A flight's origin and destination airport must be different.</summary>
public sealed class InvalidFlightRouteException(string iataCode)
    : DomainException($"A flight cannot depart from and arrive at the same airport ('{iataCode}').")
{
}

/// <summary>A flight's scheduled arrival must be after its scheduled departure.</summary>
public sealed class InvalidFlightScheduleException()
    : DomainException("Scheduled arrival must be after scheduled departure.")
{
}

/// <summary>The aircraft is not in service and so cannot be assigned to a flight.</summary>
public sealed class AircraftUnavailableException(string registration, AircraftStatus status)
    : DomainException($"Aircraft {registration} cannot be assigned: it is {status}.")
{
}

/// <summary>
/// The aircraft is already scheduled on another flight that overlaps this one, including
/// <see cref="Flight.TurnaroundBuffer"/> on both ends. Detected in Application, since it
/// requires querying every other flight assigned to the aircraft — something the domain
/// model itself has no way to do — but it is a domain-level rule, so the exception lives here.
/// </summary>
public sealed class AircraftScheduleConflictException(int aircraftId)
    : DomainException($"Aircraft {aircraftId} already has an overlapping flight scheduled.")
{
}
