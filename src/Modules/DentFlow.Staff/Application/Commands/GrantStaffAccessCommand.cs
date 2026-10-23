using ErrorOr;
using MediatR;
using DentFlow.Application.Common;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

/// <summary>Give an existing staff profile a sign-in account (people-merge plan 2.2).</summary>
public record GrantStaffAccessCommand(Guid StaffId, string Role) : IRequest<ErrorOr<GrantStaffAccessResult>>;

/// <summary>The temporary password is returned once, here, and never again.</summary>
public record GrantStaffAccessResult(Guid UserId, string TempPassword);

public class GrantStaffAccessCommandHandler(
    IStaffRepository repo,
    IUserProvisioningService provisioning,
    ITransactionRunner transaction)
    : IRequestHandler<GrantStaffAccessCommand, ErrorOr<GrantStaffAccessResult>>
{
    public async Task<ErrorOr<GrantStaffAccessResult>> Handle(GrantStaffAccessCommand command, CancellationToken ct)
    {
        if (!Roles.TenantRoles.Contains(command.Role)) return StaffErrors.InvalidAccessRole;

        var staff = await repo.GetByIdAsync(command.StaffId, ct);
        if (staff is null) return StaffErrors.NotFound;

        return await StaffAccess.GrantAsync(staff, command.Role, provisioning, transaction, repo.UpdateAsync, ct);
    }
}

/// <summary>Shared by "grant access" and "create a profile with access" (2.3).</summary>
internal static class StaffAccess
{
    public static async Task<ErrorOr<GrantStaffAccessResult>> GrantAsync(
        StaffMember staff,
        string role,
        IUserProvisioningService provisioning,
        ITransactionRunner transaction,
        Func<StaffMember, CancellationToken, Task> save,
        CancellationToken ct)
    {
        // Checked first, so no account is ever created for a profile that can't hold one.
        var eligible = staff.CanLinkUser();
        if (eligible.IsError) return eligible.Errors;

        var tempPassword = TemporaryPassword.Generate();
        return await transaction.ExecuteAsync<GrantStaffAccessResult>(async innerCt =>
        {
            // The profile's email is the login email (D1).
            var userId = await provisioning.CreateUserAsync(
                staff.Email!, tempPassword, staff.FirstName, staff.LastName, role, staff.TenantId, innerCt);
            if (userId.IsError) return userId.Errors;

            var linked = staff.LinkUser(userId.Value);
            if (linked.IsError) return linked.Errors;

            await save(staff, innerCt);
            return new GrantStaffAccessResult(userId.Value, tempPassword);
        }, ct);
    }
}
