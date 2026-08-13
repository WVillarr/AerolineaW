namespace AerolineaW.Domain.Enums;

/// <summary>
/// The lifecycle of a flight. Valid forward transitions are
/// Scheduled → Boarding → InFlight → Landed, with Delayed reachable from Scheduled or
/// Boarding and returning to Scheduled, and Cancelled reachable from any state before InFlight.
/// </summary>
public enum FlightStatus
{
    Scheduled,
    Boarding,
    InFlight,
    Landed,
    Delayed,
    Cancelled,
}
