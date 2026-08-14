using AerolineaW.Application.Aircraft;
using AerolineaW.Application.Airports;
using AerolineaW.Application.Bookings;
using AerolineaW.Application.Flights;
using AerolineaW.Application.Passengers;
using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace AerolineaW.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        // Adapt<T>() reads from this static global config, so scanning IRegister
        // implementations here is enough — no IMapper injection needed anywhere.
        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        services.AddScoped<IAirportService, AirportService>();
        services.AddScoped<IAircraftService, AircraftService>();
        services.AddScoped<IPassengerService, PassengerService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
