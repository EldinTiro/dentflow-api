using ErrorOr;
using MediatR;
using DentFlow.Treatments.Domain;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Treatments.Application.Queries;

public record ListTreatmentPlansQuery(
    Guid PatientId,
    TreatmentPlanStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IPatientScoped, IRequest<ErrorOr<PagedResult<TreatmentPlanResponse>>>;
