using AerolineaW.Domain.Enums;
using AerolineaW.Domain.Exceptions;

namespace AerolineaW.Domain.Entities;

/// <summary>A physical airplane that can be assigned to flights.</summary>
public sealed class Aircraft
{
    public int Id { get; private set; }

    public string Registration { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public int SeatCapacity { get; private set; }

    public decimal MaxPayloadKg { get; private set; }

    public AircraftStatus Status { get; private set; }

    private Aircraft()
    {
        // Reserved for EF Core materialization.
    }

    public Aircraft(string registration, string model, int seatCapacity, decimal maxPayloadKg)
    {
        if (string.IsNullOrWhiteSpace(registration))
        {
            throw new ArgumentException("Registration cannot be empty.", nameof(registration));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model cannot be empty.", nameof(model));
        }

        if (seatCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatCapacity), seatCapacity, "Seat capacity must be positive.");
        }

        if (maxPayloadKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPayloadKg), maxPayloadKg, "Maximum payload must be positive.");
        }

        Registration = registration.Trim().ToUpperInvariant();
        Model = model.Trim();
        SeatCapacity = seatCapacity;
        MaxPayloadKg = maxPayloadKg;
        Status = AircraftStatus.InService;
    }

    public void SendToMaintenance()
    {
        if (Status == AircraftStatus.Retired)
        {
            throw new AircraftUnavailableException(Registration, Status);
        }

        Status = AircraftStatus.InMaintenance;
    }

    public void ReturnToService()
    {
        if (Status == AircraftStatus.Retired)
        {
            throw new AircraftUnavailableException(Registration, Status);
        }

        Status = AircraftStatus.InService;
    }

    public void Retire() => Status = AircraftStatus.Retired;
}
