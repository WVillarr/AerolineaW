using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;
using AerolineaW.Domain.Exceptions;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Application.Flights;

public interface IFlightService
{
    Task<PagedResult<FlightResponse>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<FlightResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<FlightResponse> CreateAsync(CreateFlightRequest request, CancellationToken cancellationToken = default);

    Task<FlightResponse> AssignAircraftAsync(int id, AssignAircraftRequest request, CancellationToken cancellationToken = default);

    Task<FlightResponse> BoardAsync(int id, CancellationToken cancellationToken = default);

    Task<FlightResponse> DepartAsync(int id, RecordActualTimeRequest request, CancellationToken cancellationToken = default);

    Task<FlightResponse> LandAsync(int id, RecordActualTimeRequest request, CancellationToken cancellationToken = default);

    Task<FlightResponse> DelayAsync(int id, CancellationToken cancellationToken = default);

    Task<FlightResponse> ResumeFromDelayAsync(int id, CancellationToken cancellationToken = default);

    Task<FlightResponse> CancelAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class FlightService(IApplicationDbContext db) : IFlightService
{
    public async Task<PagedResult<FlightResponse>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = FlightQuery().OrderBy(f => f.ScheduledDepartureUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var flights = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<FlightResponse>(flights.Adapt<List<FlightResponse>>(), page.Page, page.PageSize, totalCount);
    }

    public async Task<FlightResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var flight = await FindAsync(id, cancellationToken);
        return flight.Adapt<FlightResponse>();
    }

    public async Task<FlightResponse> CreateAsync(CreateFlightRequest request, CancellationToken cancellationToken = default)
    {
        var aircraft = await db.Aircraft.FirstOrDefaultAsync(a => a.Id == request.AircraftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Aircraft), request.AircraftId);
        var origin = await db.Airports.FirstOrDefaultAsync(a => a.Id == request.OriginAirportId, cancellationToken)
            ?? throw new NotFoundException(nameof(Airport), request.OriginAirportId);
        var destination = await db.Airports.FirstOrDefaultAsync(a => a.Id == request.DestinationAirportId, cancellationToken)
            ?? throw new NotFoundException(nameof(Airport), request.DestinationAirportId);

        await EnsureNoScheduleConflictAsync(
            aircraft.Id, request.ScheduledDepartureUtc, request.ScheduledArrivalUtc, excludeFlightId: null, cancellationToken);

        var flight = new Flight(
            request.FlightNumber, aircraft, origin, destination, request.ScheduledDepartureUtc, request.ScheduledArrivalUtc);
        db.Flights.Add(flight);
        await db.SaveChangesAsync(cancellationToken);

        return flight.Adapt<FlightResponse>();
    }

    public async Task<FlightResponse> AssignAircraftAsync(int id, AssignAircraftRequest request, CancellationToken cancellationToken = default)
    {
        var flight = await FindAsync(id, cancellationToken);
        var aircraft = await db.Aircraft.FirstOrDefaultAsync(a => a.Id == request.AircraftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Aircraft), request.AircraftId);

        await EnsureNoScheduleConflictAsync(
            aircraft.Id, flight.ScheduledDepartureUtc, flight.ScheduledArrivalUtc, excludeFlightId: flight.Id, cancellationToken);

        flight.AssignAircraft(aircraft);
        await db.SaveChangesAsync(cancellationToken);
        return flight.Adapt<FlightResponse>();
    }

    public Task<FlightResponse> BoardAsync(int id, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, f => f.Board(), cancellationToken);

    public Task<FlightResponse> DepartAsync(int id, RecordActualTimeRequest request, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, f => f.Depart(request.ActualTimeUtc ?? DateTime.UtcNow), cancellationToken);

    public Task<FlightResponse> LandAsync(int id, RecordActualTimeRequest request, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, f => f.Land(request.ActualTimeUtc ?? DateTime.UtcNow), cancellationToken);

    public Task<FlightResponse> DelayAsync(int id, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, f => f.Delay(), cancellationToken);

    public Task<FlightResponse> ResumeFromDelayAsync(int id, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, f => f.ResumeFromDelay(), cancellationToken);

    public Task<FlightResponse> CancelAsync(int id, CancellationToken cancellationToken = default) =>
        ApplyAsync(id, f => f.Cancel(), cancellationToken, includeBookings: true);

    private async Task<FlightResponse> ApplyAsync(
        int id, Action<Flight> transition, CancellationToken cancellationToken, bool includeBookings = false)
    {
        var flight = await FindAsync(id, cancellationToken, includeBookings);
        transition(flight);
        await db.SaveChangesAsync(cancellationToken);
        return flight.Adapt<FlightResponse>();
    }

    private async Task EnsureNoScheduleConflictAsync(
        int aircraftId, DateTime departureUtc, DateTime arrivalUtc, int? excludeFlightId, CancellationToken cancellationToken)
    {
        var candidates = await db.Flights
            .Where(f => f.AircraftId == aircraftId && f.Status != FlightStatus.Cancelled && f.Id != (excludeFlightId ?? 0))
            .ToListAsync(cancellationToken);

        if (candidates.Any(f => f.OverlapsWithTurnaround(departureUtc, arrivalUtc)))
        {
            throw new AircraftScheduleConflictException(aircraftId);
        }
    }

    private IQueryable<Flight> FlightQuery(bool includeBookings = false)
    {
        var query = db.Flights
            .Include(f => f.Aircraft)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport);

        return includeBookings ? query.Include(f => f.Bookings) : query;
    }

    private async Task<Flight> FindAsync(int id, CancellationToken cancellationToken, bool includeBookings = false) =>
        await FlightQuery(includeBookings).FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Flight), id);
}
