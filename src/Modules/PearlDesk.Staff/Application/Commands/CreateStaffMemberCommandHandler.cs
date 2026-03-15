﻿using ErrorOr;
using MediatR;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Commands;

public class CreateStaffMemberCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<CreateStaffMemberCommand, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(
        CreateStaffMemberCommand command,
        CancellationToken cancellationToken)
    {

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

        await staffRepository.AddAsync(staffMember, cancellationToken);

        return StaffMemberResponse.FromEntity(staffMember);
    }
}

