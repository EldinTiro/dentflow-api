using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Queries;

public record GetMedicalHistoryQuery(Guid PatientId)
    : IRequest<ErrorOr<MedicalHistoryResponse?>>;
