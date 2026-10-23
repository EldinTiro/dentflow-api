using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class DeleteStaffMemberCommandHandler(
    IStaffRepository staffRepository,
    IUserAccountLifecycle accounts,
    ITransactionRunner transaction)
    : IRequestHandler<DeleteStaffMemberCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteStaffMemberCommand command,
        CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(command.Id, cancellationToken);
        if (staffMember is null)
            return StaffErrors.NotFound;

        // The account follows the profile (people-merge plan D4), or neither changes.
        return await transaction.ExecuteAsync<Deleted>(async ct =>
        {
            if (staffMember.UserId is { } userId)
            {
                var deactivated = await accounts.DeactivateAsync(userId, command.Actor, ct);
                if (deactivated.IsError) return deactivated.Errors;
            }

            await staffRepository.SoftDeleteAsync(staffMember, ct);
            return Result.Deleted;
        }, cancellationToken);
    }
}
