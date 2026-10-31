using FluentValidation;

namespace DentFlow.Appointments.Application.Commands;

public class CreateAppointmentTypeCommandValidator : AbstractValidator<CreateAppointmentTypeCommand>
{
    public CreateAppointmentTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DefaultDurationMinutes).InclusiveBetween(1, 480);
        RuleFor(x => x.ColorHex)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("ColorHex must be a valid hex colour (e.g. #3B82F6).")
            .When(x => x.ColorHex is not null);
        RuleFor(x => x.DefaultFee)
            .InclusiveBetween(0m, 100_000m)
            .WithMessage("The default fee must be between 0 and 100,000.")
            .When(x => x.DefaultFee.HasValue);
    }
}

public class UpdateAppointmentTypeCommandValidator : AbstractValidator<UpdateAppointmentTypeCommand>
{
    public UpdateAppointmentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DefaultDurationMinutes).InclusiveBetween(1, 480);
        RuleFor(x => x.ColorHex)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("ColorHex must be a valid hex colour (e.g. #3B82F6).")
            .When(x => x.ColorHex is not null);
        RuleFor(x => x.DefaultFee)
            .InclusiveBetween(0m, 100_000m)
            .WithMessage("The default fee must be between 0 and 100,000.")
            .When(x => x.DefaultFee.HasValue);
    }
}
