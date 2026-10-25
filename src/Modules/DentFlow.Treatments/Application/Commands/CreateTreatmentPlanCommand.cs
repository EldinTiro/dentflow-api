using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Treatments.Application.Commands;

public record CreateTreatmentPlanCommand(
    Guid PatientId,
    string Title,
    string? Notes
) : IPatientScoped, IRequest<ErrorOr<TreatmentPlanResponse>>;
