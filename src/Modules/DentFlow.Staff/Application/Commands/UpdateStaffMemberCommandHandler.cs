using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class UpdateStaffMemberCommandHandler(
    IStaffRepository staffRepository,
    IUserAccountLifecycle accounts,
    ITransactionRunner transaction,
    IAppointmentReader appointments)
    : IRequestHandler<UpdateStaffMemberCommand, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(
        UpdateStaffMemberCommand command,
        CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(command.Id, cancellationToken);
        if (staffMember is null)
            return StaffErrors.NotFound;

        // While linked, the email is the login email (people-merge plan D1).
        if (staffMember.UserId is not null &&
            !string.Equals(staffMember.Email, command.Email, StringComparison.OrdinalIgnoreCase))
            return StaffErrors.EmailLockedByAccess;

        // No longer bookable: visits ahead would be left without someone who can take them
        if (command.StaffType is { } newType && staffMember.IsBookable && !StaffMember.IsClinical(newType))
        {
            var upcoming = await appointments.CountUpcomingForProviderAsync(staffMember.Id, DateTime.UtcNow, cancellationToken);
            if (upcoming > 0) return StaffErrors.HasUpcomingAppointments(upcoming);
        }

        return await transaction.ExecuteAsync<StaffMemberResponse>(async ct =>
        {
            staffMember.Update(
                command.FirstName,
                command.LastName,
                staffMember.UserId is null ? command.Email : staffMember.Email,
                command.Phone,
                command.Specialty,
                command.ColorHex,
                command.Biography,
                command.LicenseNumber,
                command.LicenseExpiry,
                command.NpiNumber,
                command.Address,
                command.City,
                command.PostalCode);
            if (command.StaffType is { } type)
                staffMember.ChangeType(type);

            // One name for profile and account (D6).
            if (staffMember.UserId is { } userId)
            {
                var renamed = await accounts.RenameAsync(userId, staffMember.FirstName, staffMember.LastName, ct);
                if (renamed.IsError) return renamed.Errors;
            }

            await staffRepository.UpdateAsync(staffMember, ct);
            return StaffMemberResponse.FromEntity(staffMember);
        }, cancellationToken);
    }
}
