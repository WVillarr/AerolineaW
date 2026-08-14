using FluentValidation;

namespace AerolineaW.Application.Aircraft;

public sealed class CreateAircraftRequestValidator : AbstractValidator<CreateAircraftRequest>
{
    public CreateAircraftRequestValidator()
    {
        RuleFor(x => x.Registration).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SeatCapacity).GreaterThan(0);
        RuleFor(x => x.MaxPayloadKg).GreaterThan(0);
    }
}
