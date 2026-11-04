using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Queries;

/// <summary>Visits, treatment, invoices, payments, files and tooth findings, newest first.</summary>
public record GetPatientTimelineQuery(Guid PatientId, int Limit = 50)
    : IPatientScoped, IRequest<ErrorOr<IReadOnlyList<PatientTimelineEntry>>>;

public class GetPatientTimelineQueryHandler(IPatientTimelineReader timeline)
    : IRequestHandler<GetPatientTimelineQuery, ErrorOr<IReadOnlyList<PatientTimelineEntry>>>
{
    public async Task<ErrorOr<IReadOnlyList<PatientTimelineEntry>>> Handle(GetPatientTimelineQuery query, CancellationToken ct)
    {
        var entries = await timeline.GetAsync(query.PatientId, query.Limit, ct);
        return ErrorOrFactory.From(entries);
    }
}
