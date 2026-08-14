namespace AerolineaW.Application.Airports;

public sealed record AirportResponse(int Id, string Code, string Name, string City, string Country, string TimeZoneId);

public sealed record CreateAirportRequest(string Code, string Name, string City, string Country, string TimeZoneId);

public sealed record UpdateAirportRequest(string Name, string City, string Country, string TimeZoneId);
