using FluentValidation;

namespace DentFlow.Treatments.Application.Commands;

public class AddTreatmentPlanItemCommandValidator : AbstractValidator<AddTreatmentPlanItemCommand>
{
    public AddTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.TreatmentPlanId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Fee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ToothNumber)
            .InclusiveBetween(1, 32)
            .WithMessage("Tooth number must be between 1 and 32.")
            .When(x => x.ToothNumber.HasValue);
        RuleFor(x => x.Surface).MaximumLength(20).When(x => x.Surface is not null);
        RuleFor(x => x.CdtCode).MaximumLength(20).When(x => x.CdtCode is not null);
        RuleFor(x => x.Status).IsInEnum();
    }
}
