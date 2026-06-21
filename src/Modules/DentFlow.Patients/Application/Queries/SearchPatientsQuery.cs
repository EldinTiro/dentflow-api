using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Queries;

public record PatientSearchResult(
    Guid Id,
    string PatientNumber,
    string FullName,
    string? PhoneMobile,
    string? Email);

public record SearchPatientsQuery(string? Q, int Limit = 20)
    : IRequest<ErrorOr<IReadOnlyList<PatientSearchResult>>>;
