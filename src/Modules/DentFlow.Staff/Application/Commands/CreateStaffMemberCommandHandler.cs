using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class CreateStaffMemberCommandHandler(
    IStaffRepository staffRepository,
    IUserProvisioningService provisioning,
    ITransactionRunner transaction)
    : IRequestHandler<CreateStaffMemberCommand, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(
        CreateStaffMemberCommand command,
        CancellationToken cancellationToken)
    {
        // Nothing is written unless the whole request can succeed.
        if (command.Access is { } access)
        {
            if (!Roles.TenantRoles.Contains(access.Role)) return StaffErrors.InvalidAccessRole;
            if (string.IsNullOrWhiteSpace(command.Email)) return StaffErrors.EmailRequiredForAccess;
        }

        var existing = await staffRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existing is not null)
            return StaffErrors.AlreadyExists;

        var staffMember = StaffMember.Create(
            command.StaffType,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.HireDate,
            command.Specialty,
            command.ColorHex);

        if (command.Access is null)
        {
            await staffRepository.AddAsync(staffMember, cancellationToken);
            return StaffMemberResponse.FromEntity(staffMember);
        }

        // Profile, account and link commit together or not at all.
        return await transaction.ExecuteAsync<StaffMemberResponse>(async ct =>
        {
            // Saving first gives the profile its clinic (TenantId) for the account.
            await staffRepository.AddAsync(staffMember, ct);

            var granted = await StaffAccess.GrantAsync(
                staffMember, command.Access.Role, provisioning, transaction, staffRepository.UpdateAsync, ct);
            if (granted.IsError) return granted.Errors;

            return StaffMemberResponse.FromEntity(staffMember) with { TempPassword = granted.Value.TempPassword };
        }, cancellationToken);
    }
}
