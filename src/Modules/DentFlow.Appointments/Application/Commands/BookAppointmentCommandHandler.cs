using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Application.Commands;

public class BookAppointmentCommandHandler(
    IAppointmentRepository appointmentRepository,
    IAppointmentTypeRepository appointmentTypeRepository,
    IProviderBlockedTimeChecker blockedTimeChecker)
    : IRequestHandler<BookAppointmentCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EndAt <= command.StartAt)
            return AppointmentErrors.InvalidTimeRange;

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

