using FluentValidation;

namespace AerolineaW.Application.Passengers;

public sealed class CreatePassengerRequestValidator : AbstractValidator<CreatePassengerRequest>
{
    public CreatePassengerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
    }
}

public sealed class UpdatePassengerContactRequestValidator : AbstractValidator<UpdatePassengerContactRequest>
{
    public UpdatePassengerContactRequestValidator()
    {
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
    }
}
