using FluentValidation;

namespace DentFlow.Staff.Application.Commands;

public class SetStaffAvailabilityCommandValidator : AbstractValidator<SetStaffAvailabilityCommand>
{
    public SetStaffAvailabilityCommandValidator()
    {
        RuleFor(x => x.StaffMemberId).NotEmpty();

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Start time must be before end time.");

        RuleFor(x => x.EffectiveFrom).NotEmpty();

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .WithMessage("EffectiveTo must be after EffectiveFrom.")
            .When(x => x.EffectiveTo.HasValue);
    }
}
