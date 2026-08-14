using AerolineaW.Domain.Enums;

namespace AerolineaW.Application.Passengers;

public sealed record PassengerResponse(
    int Id,
    string FullName,
    DocumentType DocumentType,
    string DocumentNumber,
    string Country,
    string Phone);

public sealed record CreatePassengerRequest(
    string FullName,
    DocumentType DocumentType,
    string DocumentNumber,
    string Country,
    string Phone);

public sealed record UpdatePassengerContactRequest(string Country, string Phone);
