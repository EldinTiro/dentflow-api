using FluentValidation;

namespace DentFlow.Patients.Application.Commands;

public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneMobile).ValidPhone();
        RuleFor(x => x.PhoneHome).ValidPhone();
        RuleFor(x => x.PhoneWork).ValidPhone();
    }
}
