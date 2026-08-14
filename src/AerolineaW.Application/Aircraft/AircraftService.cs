using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Application.Aircraft;

public interface IAircraftService
{
    Task<IReadOnlyList<AircraftResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AircraftResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AircraftResponse> CreateAsync(CreateAircraftRequest request, CancellationToken cancellationToken = default);

    Task<AircraftResponse> SendToMaintenanceAsync(int id, CancellationToken cancellationToken = default);

    Task<AircraftResponse> ReturnToServiceAsync(int id, CancellationToken cancellationToken = default);

    Task<AircraftResponse> RetireAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class AircraftService(IApplicationDbContext db) : IAircraftService
{
    public async Task<IReadOnlyList<AircraftResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var aircraft = await db.Aircraft.OrderBy(a => a.Id).ToListAsync(cancellationToken);
        return aircraft.Adapt<List<AircraftResponse>>();
    }

    public async Task<AircraftResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var aircraft = await FindAsync(id, cancellationToken);
        return aircraft.Adapt<AircraftResponse>();
    }

    public async Task<AircraftResponse> CreateAsync(CreateAircraftRequest request, CancellationToken cancellationToken = default)
    {
        var aircraft = new AerolineaW.Domain.Entities.Aircraft(request.Registration, request.Model, request.SeatCapacity, request.MaxPayloadKg);
        db.Aircraft.Add(aircraft);
        await db.SaveChangesAsync(cancellationToken);
        return aircraft.Adapt<AircraftResponse>();
    }

    public Task<AircraftResponse> SendToMaintenanceAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, a => a.SendToMaintenance(), cancellationToken);

    public Task<AircraftResponse> ReturnToServiceAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, a => a.ReturnToService(), cancellationToken);

    public Task<AircraftResponse> RetireAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, a => a.Retire(), cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var aircraft = await FindAsync(id, cancellationToken);
        db.Aircraft.Remove(aircraft);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Fully qualified for the same reason as IApplicationDbContext.Aircraft: this file's own
    // namespace is AerolineaW.Application.Aircraft, which shadows the bare "Aircraft" entity type.
    private async Task<AircraftResponse> ChangeStatusAsync(int id, Action<AerolineaW.Domain.Entities.Aircraft> transition, CancellationToken cancellationToken)
    {
        var aircraft = await FindAsync(id, cancellationToken);
        transition(aircraft);
        await db.SaveChangesAsync(cancellationToken);
        return aircraft.Adapt<AircraftResponse>();
    }

    private async Task<AerolineaW.Domain.Entities.Aircraft> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Aircraft.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Aircraft), id);
}
