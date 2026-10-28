using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Application.Commands;

public class BookAppointmentCommandHandler(
    IAppointmentRepository appointmentRepository,
    IAppointmentTypeRepository appointmentTypeRepository,
    IProviderBlockedTimeChecker blockedTimeChecker,
    IPatientDirectory patients,
    IStaffDirectory staff)
    : IRequestHandler<BookAppointmentCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EndAt <= command.StartAt)
            return AppointmentErrors.InvalidTimeRange;

        if (await patients.IsInactiveAsync(command.PatientId, cancellationToken))
            return AppointmentErrors.PatientInactive;

        // The provider must be one of this clinic's staff (DT-01/02).
        var provider = await staff.GetBookabilityAsync(command.ProviderId, cancellationToken);
        if (provider is null)
            return AppointmentErrors.ProviderNotFound;
        // DT-08: the booking UI already hides these; the API has to refuse them too (D11, D3).
        if (!provider.IsActive)
            return AppointmentErrors.ProviderInactive;
        if (!provider.IsBookable)
            return AppointmentErrors.ProviderNotBookable;

        var appointmentType = await appointmentTypeRepository.GetByIdAsync(command.AppointmentTypeId, cancellationToken);
        if (appointmentType is null)
            return AppointmentErrors.AppointmentTypeNotFound;

        var isBlocked = await blockedTimeChecker.IsProviderBlockedAsync(
            command.ProviderId, command.StartAt, command.EndAt, cancellationToken);
        if (isBlocked)
            return AppointmentErrors.ProviderUnavailable;

        var hasConflict = await appointmentRepository.HasProviderConflictAsync(
            command.ProviderId, command.StartAt, command.EndAt, null, cancellationToken);
        if (hasConflict)
            return AppointmentErrors.ProviderConflict;

        if (await appointmentRepository.HasPatientConflictAsync(
                command.PatientId, command.StartAt, command.EndAt, null, cancellationToken))
            return AppointmentErrors.PatientConflict;

        var appointment = Appointment.Create(
            command.PatientId,
            command.ProviderId,
            command.AppointmentTypeId,
            command.StartAt,
            command.EndAt,
            command.ChiefComplaint,
            command.Notes,
            command.OperatoryId,
            command.IsNewPatient,
            command.Source);

        await appointmentRepository.AddAsync(appointment, cancellationToken);

        return AppointmentResponse.FromEntity(appointment);
    }
}

