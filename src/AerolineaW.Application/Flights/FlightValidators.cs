using FluentValidation;

namespace AerolineaW.Application.Flights;

public sealed class CreateFlightRequestValidator : AbstractValidator<CreateFlightRequest>
{
    public CreateFlightRequestValidator()
    {
        RuleFor(x => x.FlightNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AircraftId).GreaterThan(0);
        RuleFor(x => x.OriginAirportId).GreaterThan(0);
        RuleFor(x => x.DestinationAirportId).GreaterThan(0);
        RuleFor(x => x.ScheduledDepartureUtc).NotEmpty();
        RuleFor(x => x.ScheduledArrivalUtc).NotEmpty();
    }
}

public sealed class AssignAircraftRequestValidator : AbstractValidator<AssignAircraftRequest>
{
    public AssignAircraftRequestValidator()
    {
        RuleFor(x => x.AircraftId).GreaterThan(0);
    }
}
