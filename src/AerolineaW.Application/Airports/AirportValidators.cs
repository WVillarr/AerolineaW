using FluentValidation;

namespace AerolineaW.Application.Airports;

public sealed class CreateAirportRequestValidator : AbstractValidator<CreateAirportRequest>
{
    public CreateAirportRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(3);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TimeZoneId).NotEmpty();
    }
}

public sealed class UpdateAirportRequestValidator : AbstractValidator<UpdateAirportRequest>
{
    public UpdateAirportRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TimeZoneId).NotEmpty();
    }
}
