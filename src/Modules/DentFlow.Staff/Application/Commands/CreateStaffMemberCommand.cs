using ErrorOr;
using MediatR;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public record CreateStaffMemberCommand(
    StaffType StaffType,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateOnly? HireDate,
    string? Specialty,
    string? ColorHex,
    StaffAccessRequest? Access = null) : IRequest<ErrorOr<StaffMemberResponse>>;

/// <summary>"Can sign in": create the account together with the profile (people-merge plan 2.3).</summary>
public record StaffAccessRequest(string Role);
