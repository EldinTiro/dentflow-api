using ErrorOr;
using MediatR;

namespace PearlDesk.Appointments.Application.Commands;

public record BookAppointmentCommand(
    Guid PatientId,
    Guid ProviderId,
    Guid AppointmentTypeId,
    DateTime StartAt,
    DateTime EndAt,
    string? ChiefComplaint,
    string? Notes,
    Guid? OperatoryId,
    bool IsNewPatient,
    string Source) : IRequest<ErrorOr<AppointmentResponse>>;

