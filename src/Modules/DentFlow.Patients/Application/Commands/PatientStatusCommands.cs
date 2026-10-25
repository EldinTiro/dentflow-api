using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public record DeactivatePatientCommand(Guid Id, string? Reason) : IRequest<ErrorOr<PatientResponse>>;

public record ReactivatePatientCommand(Guid Id) : IRequest<ErrorOr<PatientResponse>>;

public class DeactivatePatientCommandValidator : AbstractValidator<DeactivatePatientCommand>
{
    public DeactivatePatientCommandValidator() => RuleFor(c => c.Reason).MaximumLength(500);
}

/// <summary>
/// People-merge plan D10: a patient with appointments still ahead is not deactivated —
/// the clinic cancels or moves them first, so nobody turns up for a hidden patient.
/// </summary>
public class DeactivatePatientCommandHandler(IPatientRepository patients, IAppointmentReader appointments)
    : IRequestHandler<DeactivatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(DeactivatePatientCommand command, CancellationToken ct)
    {
        var patient = await patients.GetByIdAsync(command.Id, ct);
        if (patient is null) return PatientErrors.NotFound;
        if (patient.Status == PatientStatus.Inactive) return PatientErrors.AlreadyInactive;

        var now = DateTime.UtcNow;
        var upcoming = await appointments.CountUpcomingForPatientAsync(patient.Id, now, ct);
        if (upcoming > 0) return PatientErrors.HasUpcomingAppointments(upcoming);

        var deactivated = patient.Deactivate(command.Reason, now);
        if (deactivated.IsError) return deactivated.Errors;

        await patients.UpdateAsync(patient, ct);
        return PatientResponse.FromEntity(patient);
    }
}

public class ReactivatePatientCommandHandler(IPatientRepository patients)
    : IRequestHandler<ReactivatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(ReactivatePatientCommand command, CancellationToken ct)
    {
        var patient = await patients.GetByIdAsync(command.Id, ct);
        if (patient is null) return PatientErrors.NotFound;

        var reactivated = patient.Reactivate();
        if (reactivated.IsError) return reactivated.Errors;

        await patients.UpdateAsync(patient, ct);
        return PatientResponse.FromEntity(patient);
    }
}
