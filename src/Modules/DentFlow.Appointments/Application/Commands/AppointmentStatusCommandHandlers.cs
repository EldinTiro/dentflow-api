using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Application.Commands;

public class RescheduleAppointmentCommandHandler(
    IAppointmentRepository repo,
    IProviderBlockedTimeChecker blockedTimeChecker,
    IStaffDirectory staff)
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

        // Reschedule resets the status to Scheduled, so on a started or finished visit
        // it would silently rewrite clinical history.
        if (!AppointmentStatus.CanReschedule(appointment.Status))
            return AppointmentErrors.CannotReschedule;

        // Dragged to another doctor's column: they get the same checks as at booking (DT-01/02, DT-08)
        var providerId = command.NewProviderId ?? appointment.ProviderId;
        if (providerId != appointment.ProviderId)
        {
            var provider = await staff.GetBookabilityAsync(providerId, cancellationToken);
            if (provider is null)
                return AppointmentErrors.ProviderNotFound;
            if (!provider.IsActive)
                return AppointmentErrors.ProviderInactive;
            if (!provider.IsBookable)
                return AppointmentErrors.ProviderNotBookable;
        }

        var isBlocked = await blockedTimeChecker.IsProviderBlockedAsync(
            providerId, command.NewStartAt, command.NewEndAt, cancellationToken);
        if (isBlocked)
            return AppointmentErrors.ProviderUnavailable;

        var hasConflict = await repo.HasProviderConflictAsync(
            providerId, command.NewStartAt, command.NewEndAt, command.Id, cancellationToken);
        if (hasConflict)
            return AppointmentErrors.ProviderConflict;

        if (await repo.HasPatientConflictAsync(
                appointment.PatientId, command.NewStartAt, command.NewEndAt, command.Id, cancellationToken))
            return AppointmentErrors.PatientConflict;

        appointment.Reschedule(command.NewStartAt, command.NewEndAt, providerId);
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

        if (AppointmentStatus.IsTerminal(appointment.Status))
            return AppointmentErrors.AlreadyTerminal;

        if (!AppointmentStatus.CanTransitionTo(appointment.Status, AppointmentStatus.Cancelled))
            return AppointmentErrors.InvalidTransition(appointment.Status, AppointmentStatus.Cancelled);

        var fromStatus = appointment.Status;
        appointment.Cancel(command.Reason, command.CancelledByUserId);

        var history = AppointmentStatusHistory.Create(
            appointment.Id, fromStatus, AppointmentStatus.Cancelled,
            command.CancelledByUserId, command.Reason);
        await repo.AddHistoryAsync(history, cancellationToken);
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
        if (!AppointmentStatus.IsValid(command.NewStatus))
            return Error.Validation("Appointment.InvalidStatus", $"'{command.NewStatus}' is not a valid status.");

        var appointment = await repo.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        if (AppointmentStatus.IsTerminal(appointment.Status))
            return AppointmentErrors.AlreadyTerminal;

        if (!AppointmentStatus.CanTransitionTo(appointment.Status, command.NewStatus))
            return AppointmentErrors.InvalidTransition(appointment.Status, command.NewStatus);

        // DT-09: no check-in, treatment or no-show for a visit days away.
        if (command.NewStatus is not AppointmentStatus.Cancelled
            && DateTime.UtcNow < appointment.StartAt - AppointmentLimits.ArrivalWindowBeforeStart)
            return AppointmentErrors.NotYetDue;

        var fromStatus = appointment.Status;

        switch (command.NewStatus)
        {
            case AppointmentStatus.CheckedIn:  appointment.CheckIn(); break;
            case AppointmentStatus.InProgress: appointment.Start();   break;
            case AppointmentStatus.Completed:  appointment.Complete(); break;
            case AppointmentStatus.NoShow:     appointment.MarkNoShow(); break;
            case AppointmentStatus.Cancelled:
                appointment.Cancel(null, command.ChangedByUserId); break;
        }

        var history = AppointmentStatusHistory.Create(
            appointment.Id, fromStatus, command.NewStatus, command.ChangedByUserId);
        await repo.AddHistoryAsync(history, cancellationToken);
        await repo.UpdateAsync(appointment, cancellationToken);

        return AppointmentResponse.FromEntity(appointment);
    }
}

public class OverrideAppointmentStatusCommandHandler(IAppointmentRepository repo)
    : IRequestHandler<OverrideAppointmentStatusCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        OverrideAppointmentStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!AppointmentStatus.IsValid(command.NewStatus))
            return Error.Validation("Appointment.InvalidStatus", $"'{command.NewStatus}' is not a valid status.");

        var appointment = await repo.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        // An override to the status it already has would only leave an empty audit entry.
        if (appointment.Status == command.NewStatus)
            return AppointmentErrors.SameStatus;

        var fromStatus = appointment.Status;
        appointment.ForceStatus(command.NewStatus);

        var history = AppointmentStatusHistory.Create(
            appointment.Id, fromStatus, command.NewStatus,
            command.OverriddenByUserId, command.Reason, isOverride: true);
        await repo.AddHistoryAsync(history, cancellationToken);
        await repo.UpdateAsync(appointment, cancellationToken);

        return AppointmentResponse.FromEntity(appointment);
    }
}

public class UpdateAppointmentNotesCommandHandler(IAppointmentRepository repo)
    : IRequestHandler<UpdateAppointmentNotesCommand, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        UpdateAppointmentNotesCommand command,
        CancellationToken cancellationToken)
    {
        var appointment = await repo.GetByIdAsync(command.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        appointment.UpdateNotes(command.Notes);
        await repo.UpdateAsync(appointment, cancellationToken);

        return AppointmentResponse.FromEntity(appointment);
    }
}


