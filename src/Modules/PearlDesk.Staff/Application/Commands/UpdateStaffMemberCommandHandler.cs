using ErrorOr;
using MediatR;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Commands;

public class UpdateStaffMemberCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<UpdateStaffMemberCommand, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(
        UpdateStaffMemberCommand command,
        CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(command.Id, cancellationToken);
        if (staffMember is null)
            return StaffErrors.NotFound;

        staffMember.Update(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.Specialty,
            command.ColorHex,
            command.Biography,
            command.LicenseNumber,
            command.LicenseExpiry,
            command.NpiNumber);

        await staffRepository.UpdateAsync(staffMember, cancellationToken);

        return StaffMemberResponse.FromEntity(staffMember);
    }
}

