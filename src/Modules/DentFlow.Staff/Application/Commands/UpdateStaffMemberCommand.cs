using ErrorOr;
using MediatR;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public record UpdateStaffMemberCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Specialty,
    string? ColorHex,
    string? Biography,
    string? LicenseNumber,
    DateOnly? LicenseExpiry,
    string? NpiNumber,
    string? Address,
    string? City,
    string? PostalCode,
    StaffType? StaffType = null) : IRequest<ErrorOr<StaffMemberResponse>>;

