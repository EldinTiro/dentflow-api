using FluentValidation;
using DentFlow.Domain.Clinical;

namespace DentFlow.Treatments.Application.Commands;

/// <summary>Same rules as adding an item; editing went unchecked before (any tooth number passed).</summary>
public class UpdateTreatmentPlanItemCommandValidator : AbstractValidator<UpdateTreatmentPlanItemCommand>
{
    public UpdateTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Fee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ToothNumber)
            .Must(t => Fdi.IsPermanentTooth(t!.Value))
            .WithMessage("Use an FDI tooth number: 11–18, 21–28, 31–38 or 41–48.")
            .When(x => x.ToothNumber.HasValue);
        RuleFor(x => x.Surface).MaximumLength(20).When(x => x.Surface is not null);
        RuleFor(x => x.CdtCode).MaximumLength(20).When(x => x.CdtCode is not null);
        RuleFor(x => x.Status).IsInEnum();
    }
}
