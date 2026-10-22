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
    }
}

