using AerolineaW.Domain.Enums;

namespace AerolineaW.Application.Aircraft;

public sealed record AircraftResponse(
    int Id,
    string Registration,
    string Model,
    int SeatCapacity,
    decimal MaxPayloadKg,
    AircraftStatus Status);

public sealed record CreateAircraftRequest(string Registration, string Model, int SeatCapacity, decimal MaxPayloadKg);
