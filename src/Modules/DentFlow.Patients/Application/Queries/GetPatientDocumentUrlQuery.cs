using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Queries;

public record GetPatientDocumentUrlQuery(
    Guid PatientId,
    Guid DocumentId) : IPatientScoped, IRequest<ErrorOr<string>>;
