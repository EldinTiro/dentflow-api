using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

/// <summary>The Administrator deletes a staff profile for good, with its sign-in.</summary>
public record PurgeStaffMemberCommand(Guid Id, AccountActor Actor) : IRequest<ErrorOr<Deleted>>;

/// <summary>
/// User decision 2026-11-13: only a profile with no appointments; a doctor's visits belong to
/// the patients' records too, so anyone who has worked is deactivated instead.
/// </summary>
public class PurgeStaffMemberCommandHandler(
    IStaffRepository staffRepository, IAppointmentReader appointments, IPeoplePurger purger, ITransactionRunner transaction)
    : IRequestHandler<PurgeStaffMemberCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(PurgeStaffMemberCommand command, CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(command.Id, cancellationToken);
        if (staffMember is null) return StaffErrors.NotFound;
        if (staffMember.UserId is { } userId && userId == command.Actor.UserId) return StaffErrors.CannotDeleteSelf;
        if (await appointments.ProviderHasAnyAsync(staffMember.Id, cancellationToken)) return StaffErrors.HasHistory;

        return await transaction.ExecuteAsync<Deleted>(async ct =>
        {
            await purger.PurgeStaffMemberAsync(staffMember.Id, staffMember.UserId, ct);
            return Result.Deleted;
        }, cancellationToken);
    }
}
