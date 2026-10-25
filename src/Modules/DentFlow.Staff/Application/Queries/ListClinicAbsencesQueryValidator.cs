using FluentValidation;

namespace DentFlow.Staff.Application.Queries;

public class ListClinicAbsencesQueryValidator : AbstractValidator<ListClinicAbsencesQuery>
{
    public ListClinicAbsencesQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .WithMessage("'to' must be after 'from'.");
    }
}
