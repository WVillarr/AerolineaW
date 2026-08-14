using AerolineaW.Application.Aircraft;
using AerolineaW.Application.Airports;
using AerolineaW.Application.Bookings;
using AerolineaW.Application.Flights;
using AerolineaW.Domain.Entities;
using Mapster;

namespace AerolineaW.Application.Mapping;

/// <summary>
/// Explicit Mapster configuration for the mappings that need it: extracting the raw string out
/// of a value object, or flattening a navigation property. Aircraft-&gt;AircraftResponse and
/// Passenger-&gt;PassengerResponse aren't registered here at all — their DTOs match the entity's
/// property names and types exactly, so Mapster's convention-based mapping already gets them
/// right without any configuration to maintain.
/// </summary>
public sealed class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Airport, AirportResponse>()
            .Map(dest => dest.Code, src => src.Code.Value);

        config.NewConfig<Flight, FlightResponse>()
            .Map(dest => dest.AircraftRegistration, src => src.Aircraft.Registration)
            .Map(dest => dest.OriginAirportCode, src => src.OriginAirport.Code.Value)
            .Map(dest => dest.DestinationAirportCode, src => src.DestinationAirport.Code.Value)
            .Map(dest => dest.SeatCapacity, src => src.Aircraft.SeatCapacity);

        config.NewConfig<Booking, BookingResponse>()
            .Map(dest => dest.Reference, src => src.Reference.Value)
            .Map(dest => dest.FlightNumber, src => src.Flight.FlightNumber)
            .Map(dest => dest.PassengerFullName, src => src.Passenger.FullName)
            .Map(dest => dest.Seat, src => src.Seat.Value);
    }
}
