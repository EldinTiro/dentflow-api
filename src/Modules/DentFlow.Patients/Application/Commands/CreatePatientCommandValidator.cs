using FluentValidation;

namespace DentFlow.Patients.Application.Commands;

public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneMobile).ValidPhone();
        RuleFor(x => x.PhoneHome).ValidPhone();
        RuleFor(x => x.PhoneWork).ValidPhone();
        RuleFor(x => x.CountryCode).Length(2)
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.DateOfBirth)
            .Must(d => d == null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future");
    }
}

