using AerolineaW.Application.Common;
using AerolineaW.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Application.Passengers;

public interface IPassengerService
{
    Task<IReadOnlyList<PassengerResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PassengerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PassengerResponse> CreateAsync(CreatePassengerRequest request, CancellationToken cancellationToken = default);

    Task<PassengerResponse> UpdateContactDetailsAsync(int id, UpdatePassengerContactRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PassengerService(IApplicationDbContext db) : IPassengerService
{
    public async Task<IReadOnlyList<PassengerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var passengers = await db.Passengers.OrderBy(p => p.Id).ToListAsync(cancellationToken);
        return passengers.Adapt<List<PassengerResponse>>();
    }

    public async Task<PassengerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var passenger = await FindAsync(id, cancellationToken);
        return passenger.Adapt<PassengerResponse>();
    }

    public async Task<PassengerResponse> CreateAsync(CreatePassengerRequest request, CancellationToken cancellationToken = default)
    {
        var passenger = new Passenger(request.FullName, request.DocumentType, request.DocumentNumber, request.Country, request.Phone);
        db.Passengers.Add(passenger);
        await db.SaveChangesAsync(cancellationToken);
        return passenger.Adapt<PassengerResponse>();
    }

    public async Task<PassengerResponse> UpdateContactDetailsAsync(int id, UpdatePassengerContactRequest request, CancellationToken cancellationToken = default)
    {
        var passenger = await FindAsync(id, cancellationToken);
        passenger.UpdateContactDetails(request.Country, request.Phone);
        await db.SaveChangesAsync(cancellationToken);
        return passenger.Adapt<PassengerResponse>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var passenger = await FindAsync(id, cancellationToken);
        db.Passengers.Remove(passenger);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Passenger> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Passengers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Passenger), id);
}
