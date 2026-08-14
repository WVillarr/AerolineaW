using AerolineaW.Domain.Enums;

namespace AerolineaW.Application.Flights;

public sealed record FlightResponse(
    int Id,
    string FlightNumber,
    int AircraftId,
    string AircraftRegistration,
    int OriginAirportId,
    string OriginAirportCode,
    int DestinationAirportId,
    string DestinationAirportCode,
    DateTime ScheduledDepartureUtc,
    DateTime ScheduledArrivalUtc,
    DateTime? ActualDepartureUtc,
    DateTime? ActualArrivalUtc,
    FlightStatus Status,
    int SeatsBooked,
    int SeatCapacity);

public sealed record CreateFlightRequest(
    string FlightNumber,
    int AircraftId,
    int OriginAirportId,
    int DestinationAirportId,
    DateTime ScheduledDepartureUtc,
    DateTime ScheduledArrivalUtc);

public sealed record AssignAircraftRequest(int AircraftId);

/// <summary>ActualTimeUtc is optional: when omitted, the service stamps the current time.</summary>
public sealed record RecordActualTimeRequest(DateTime? ActualTimeUtc);
