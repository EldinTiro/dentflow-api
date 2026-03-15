using ErrorOr;
using MediatR;

namespace PearlDesk.Staff.Application.Commands;

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
    string? NpiNumber) : IRequest<ErrorOr<StaffMemberResponse>>;

