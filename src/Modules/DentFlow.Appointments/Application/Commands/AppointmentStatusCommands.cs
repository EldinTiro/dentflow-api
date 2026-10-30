using ErrorOr;
using MediatR;

namespace DentFlow.Appointments.Application.Commands;

public record RescheduleAppointmentCommand(
    Guid Id,
    DateTime NewStartAt,
    DateTime NewEndAt,
    Guid? NewProviderId = null) : IRequest<ErrorOr<AppointmentResponse>>;

public record CancelAppointmentCommand(
    Guid Id,
    string? Reason,
    Guid? CancelledByUserId) : IRequest<ErrorOr<AppointmentResponse>>;

/// <summary>Normal status advancement following the allowed transition rules.</summary>
public record UpdateAppointmentStatusCommand(
    Guid Id,
    string NewStatus,
    Guid? ChangedByUserId = null) : IRequest<ErrorOr<AppointmentResponse>>;

/// <summary>Admin/owner force-override to any valid status (bypasses transition rules).</summary>
public record OverrideAppointmentStatusCommand(
    Guid Id,
    string NewStatus,
    string? Reason,
    Guid? OverriddenByUserId) : IRequest<ErrorOr<AppointmentResponse>>;

/// <summary>Update the free-text notes field on any appointment.</summary>
public record UpdateAppointmentNotesCommand(
    Guid Id,
    string? Notes,
    Guid? UpdatedByUserId) : IRequest<ErrorOr<AppointmentResponse>>;

