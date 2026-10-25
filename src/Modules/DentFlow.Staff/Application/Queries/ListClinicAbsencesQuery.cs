using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Queries;

/// <summary>Every absence in the clinic that overlaps [From, To) — UTC instants of local day boundaries.</summary>
public record ListClinicAbsencesQuery(DateTime From, DateTime To)
    : IRequest<ErrorOr<IReadOnlyList<StaffBlockedTimeResponse>>>;
