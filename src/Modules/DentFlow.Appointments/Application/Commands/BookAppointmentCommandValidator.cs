using FluentValidation;

namespace DentFlow.Appointments.Application.Commands;

public class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.AppointmentTypeId).NotEmpty();
        RuleFor(x => x.StartAt)
            .NotEmpty()
            // A little slack for clock skew and a walk-in booked as they arrive (QA-018).
            .Must(startAt => startAt >= DateTime.UtcNow.AddMinutes(-10))
            .WithMessage("An appointment cannot start in the past.");
        RuleFor(x => x.EndAt)
            .NotEmpty()
            .GreaterThan(x => x.StartAt)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x)
            .Must(x => x.EndAt - x.StartAt <= AppointmentLimits.MaxLength)
            .WithName("EndAt")
            .WithMessage("An appointment can be at most 8 hours long.");
        RuleFor(x => x.StartAt)
            .Must(startAt => startAt <= DateTime.UtcNow.AddYears(AppointmentLimits.BookingHorizonYears))
            .WithMessage("An appointment can be booked at most two years ahead.");
        RuleFor(x => x.Notes).MaximumLength(AppointmentLimits.MaxNotesLength);
        RuleFor(x => x.ChiefComplaint).MaximumLength(AppointmentLimits.MaxNotesLength);
    }
}

public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.NewEndAt).GreaterThan(x => x.NewStartAt).WithMessage("End time must be after start time.");
        RuleFor(x => x)
            .Must(x => x.NewEndAt - x.NewStartAt <= AppointmentLimits.MaxLength)
            .WithName("NewEndAt")
            .WithMessage("An appointment can be at most 8 hours long.");
        RuleFor(x => x.NewStartAt)
            .Must(startAt => startAt <= DateTime.UtcNow.AddYears(AppointmentLimits.BookingHorizonYears))
            .WithMessage("An appointment can be booked at most two years ahead.");
    }
}

public class UpdateAppointmentNotesCommandValidator : AbstractValidator<UpdateAppointmentNotesCommand>
{
    public UpdateAppointmentNotesCommandValidator() =>
        RuleFor(x => x.Notes).MaximumLength(AppointmentLimits.MaxNotesLength);
}

/// <summary>DT-09: an override bypasses the workflow, so it must say why.</summary>
public class OverrideAppointmentStatusCommandValidator : AbstractValidator<OverrideAppointmentStatusCommand>
{
    public OverrideAppointmentStatusCommandValidator() =>
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage("Give a reason for the status override.")
            .MaximumLength(500);
}
