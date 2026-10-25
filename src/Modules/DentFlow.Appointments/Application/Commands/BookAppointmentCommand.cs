using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Appointments.Application.Commands;

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
    string Source) : IPatientScoped, IRequest<ErrorOr<AppointmentResponse>>;

