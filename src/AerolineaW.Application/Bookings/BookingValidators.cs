using FluentValidation;

namespace AerolineaW.Application.Bookings;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.PassengerId).GreaterThan(0);
        RuleFor(x => x.Seat).NotEmpty().MaximumLength(4);
        RuleFor(x => x.BaggageWeightKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
