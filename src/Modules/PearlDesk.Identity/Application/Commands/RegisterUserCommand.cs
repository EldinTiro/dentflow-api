using ErrorOr;
using MediatR;

namespace PearlDesk.Identity.Application.Commands;

/// <summary>
/// Registers a new application user and assigns them the specified role.
/// Used for tenant onboarding (ClinicOwner) and SuperAdmin-driven staff provisioning.
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? TenantId) : IRequest<ErrorOr<RegisterUserResult>>;

public record RegisterUserResult(
    Guid UserId,
    string Email,
    string FullName,
    string Role);

