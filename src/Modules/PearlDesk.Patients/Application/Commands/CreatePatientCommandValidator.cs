using FluentValidation;

namespace PearlDesk.Patients.Application.Commands;

public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneMobile).MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneMobile));
        RuleFor(x => x.CountryCode).Length(2)
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.DateOfBirth)
            .Must(d => d == null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future");
    }
}

