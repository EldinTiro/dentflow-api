using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Queries;

/// <summary>The patient's tooth chart: every finding, by tooth.</summary>
public record ListToothFindingsQuery(Guid PatientId)
    : IPatientScoped, IRequest<ErrorOr<IReadOnlyList<ToothFindingResponse>>>;

public class ListToothFindingsQueryHandler(IPatientRepository repo)
    : IRequestHandler<ListToothFindingsQuery, ErrorOr<IReadOnlyList<ToothFindingResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<ToothFindingResponse>>> Handle(ListToothFindingsQuery query, CancellationToken ct)
    {
        var findings = await repo.ListToothFindingsAsync(query.PatientId, ct);
        return findings.Select(ToothFindingResponse.FromEntity).ToList();
    }
}
