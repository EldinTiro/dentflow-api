using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public record DeactivateStaffMemberCommand(Guid Id, AccountActor Actor) : IRequest<ErrorOr<StaffMemberResponse>>;

public record ReactivateStaffMemberCommand(Guid Id) : IRequest<ErrorOr<StaffMemberResponse>>;

/// <summary>
/// People-merge plan B6: someone who leaves is deactivated — their past appointments keep
/// pointing at them. Refused while appointments are still ahead (D10). A linked account is
/// deactivated in the same transaction, through the same guards as on the account side
/// (not yourself, not the clinic's last owner).
/// </summary>
public class DeactivateStaffMemberCommandHandler(
    IStaffRepository staffRepository,
    IUserAccountLifecycle accounts,
    IAppointmentReader appointments,
    ITransactionRunner transaction)
    : IRequestHandler<DeactivateStaffMemberCommand, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(DeactivateStaffMemberCommand command, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(command.Id, cancellationToken);
        if (staff is null) return StaffErrors.NotFound;
        if (!staff.IsActive) return StaffErrors.AlreadyInactive;

        var upcoming = await appointments.CountUpcomingForProviderAsync(staff.Id, DateTime.UtcNow, cancellationToken);
        if (upcoming > 0) return StaffErrors.HasUpcomingAppointments(upcoming);

        return await transaction.ExecuteAsync<StaffMemberResponse>(async ct =>
        {
            if (staff.UserId is { } userId)
            {
                var account = await accounts.DeactivateAsync(userId, command.Actor, ct);
                if (account.IsError) return account.Errors;
            }

            var deactivated = staff.Deactivate();
            if (deactivated.IsError) return deactivated.Errors;

            await staffRepository.UpdateAsync(staff, ct);
            return StaffMemberResponse.FromEntity(staff);
        }, cancellationToken);
    }
}

/// <summary>
/// Back to active. The linked account stays inactive (D4): the owner turns sign-in back on
/// explicitly, so reactivating a profile never silently restores access.
/// </summary>
public class ReactivateStaffMemberCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<ReactivateStaffMemberCommand, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(ReactivateStaffMemberCommand command, CancellationToken ct)
    {
        var staff = await staffRepository.GetByIdAsync(command.Id, ct);
        if (staff is null) return StaffErrors.NotFound;

        var activated = staff.Activate();
        if (activated.IsError) return activated.Errors;

        await staffRepository.UpdateAsync(staff, ct);
        return StaffMemberResponse.FromEntity(staff);
    }
}
