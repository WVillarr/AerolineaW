using AerolineaW.Domain.Enums;

namespace AerolineaW.Application.Bookings;

public sealed record BookingResponse(
    int Id,
    string Reference,
    int FlightId,
    string FlightNumber,
    int PassengerId,
    string PassengerFullName,
    string Seat,
    decimal BaggageWeightKg,
    decimal Price,
    BookingStatus Status,
    DateTime BookedAtUtc,
    DateTime? CheckedInAtUtc);

public sealed record CreateBookingRequest(int PassengerId, string Seat, decimal BaggageWeightKg, decimal Price);
