using ErrorOr;
using MediatR;
using PearlDesk.Appointments.Application.Interfaces;
using PearlDesk.Appointments.Domain;

namespace PearlDesk.Appointments.Application.Commands;

public class RescheduleAppointmentCommandHandler(IAppointmentRepository repo)
    : IRequestHandler<RescheduleAppointmentCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        RescheduleAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.NewEndAt <= command.NewStartAt)
            return AppointmentErrors.InvalidTimeRange;

        var appointment = await repo.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        var hasConflict = await repo.HasProviderConflictAsync(
            appointment.ProviderId, command.NewStartAt, command.NewEndAt, command.Id, cancellationToken);
        if (hasConflict)
            return AppointmentErrors.ProviderConflict;

        appointment.Reschedule(command.NewStartAt, command.NewEndAt);
        await repo.UpdateAsync(appointment, cancellationToken);

        return AppointmentResponse.FromEntity(appointment);
    }
}

public class CancelAppointmentCommandHandler(IAppointmentRepository repo)
    : IRequestHandler<CancelAppointmentCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        CancelAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var appointment = await repo.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        if (appointment.Status == AppointmentStatus.Completed)
            return AppointmentErrors.CannotCancelCompleted;

        appointment.Cancel(command.Reason, command.CancelledByUserId);
        await repo.UpdateAsync(appointment, cancellationToken);

        return AppointmentResponse.FromEntity(appointment);
    }
}

public class UpdateAppointmentStatusCommandHandler(IAppointmentRepository repo)
    : IRequestHandler<UpdateAppointmentStatusCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        UpdateAppointmentStatusCommand command,
        CancellationToken cancellationToken)
    {
        var appointment = await repo.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        switch (command.NewStatus)
        {
            case AppointmentStatus.Confirmed: appointment.Confirm(); break;
            case AppointmentStatus.CheckedIn: appointment.CheckIn(); break;
            case AppointmentStatus.Completed: appointment.Complete(); break;
            case AppointmentStatus.NoShow: appointment.MarkNoShow(); break;
            default: return Error.Validation("Appointment.InvalidStatus", $"Cannot transition to status '{command.NewStatus}' via this endpoint.");
        }

        await repo.UpdateAsync(appointment, cancellationToken);
        return AppointmentResponse.FromEntity(appointment);
    }
}

