using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Queries;

public record GetMedicalHistoryQuery(Guid PatientId)
    : IPatientScoped, IRequest<ErrorOr<MedicalHistoryResponse?>>;
