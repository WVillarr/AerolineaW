using AerolineaW.Domain.Common;

namespace AerolineaW.Domain.Exceptions;

/// <summary>The booking is not in a status that allows the requested operation.</summary>
public sealed class InvalidBookingStatusException(string message)
    : DomainException(message)
{
}

/// <summary>Check-in is only allowed within a fixed window before scheduled departure.</summary>
public sealed class CheckInWindowException(string bookingReference)
    : DomainException($"Booking {bookingReference} is outside its check-in window.")
{
}
