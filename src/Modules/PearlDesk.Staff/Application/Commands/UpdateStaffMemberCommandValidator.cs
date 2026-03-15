using FluentValidation;

namespace PearlDesk.Staff.Application.Commands;

public class UpdateStaffMemberCommandValidator : AbstractValidator<UpdateStaffMemberCommand>
{
    public UpdateStaffMemberCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.ColorHex)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("ColorHex must be a valid hex colour (e.g. #3B82F6)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex));

        RuleFor(x => x.LicenseNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.LicenseNumber));

        RuleFor(x => x.NpiNumber)
            .Length(10)
            .WithMessage("NPI number must be exactly 10 digits")
            .Matches(@"^\d{10}$")
            .When(x => !string.IsNullOrWhiteSpace(x.NpiNumber));
    }
}

