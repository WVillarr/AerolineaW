using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using AerolineaW.Domain.ValueObjects;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Application.Airports;

public interface IAirportService
{
    Task<IReadOnlyList<AirportResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AirportResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AirportResponse> CreateAsync(CreateAirportRequest request, CancellationToken cancellationToken = default);

    Task<AirportResponse> UpdateAsync(int id, UpdateAirportRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class AirportService(IApplicationDbContext db) : IAirportService
{
    public async Task<IReadOnlyList<AirportResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var airports = await db.Airports.OrderBy(a => a.Id).ToListAsync(cancellationToken);
        return airports.Adapt<List<AirportResponse>>();
    }

    public async Task<AirportResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var airport = await FindAsync(id, cancellationToken);
        return airport.Adapt<AirportResponse>();
    }

    public async Task<AirportResponse> CreateAsync(CreateAirportRequest request, CancellationToken cancellationToken = default)
    {
        var airport = new Airport(new IataCode(request.Code), request.Name, request.City, request.Country, request.TimeZoneId);
        db.Airports.Add(airport);
        await db.SaveChangesAsync(cancellationToken);
        return airport.Adapt<AirportResponse>();
    }

    public async Task<AirportResponse> UpdateAsync(int id, UpdateAirportRequest request, CancellationToken cancellationToken = default)
    {
        var airport = await FindAsync(id, cancellationToken);
        airport.SetDetails(request.Name, request.City, request.Country, request.TimeZoneId);
        await db.SaveChangesAsync(cancellationToken);
        return airport.Adapt<AirportResponse>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var airport = await FindAsync(id, cancellationToken);
        db.Airports.Remove(airport);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Airport> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Airports.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Airport), id);
}
