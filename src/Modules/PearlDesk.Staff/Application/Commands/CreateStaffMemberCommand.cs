﻿using ErrorOr;
using MediatR;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Commands;

public record CreateStaffMemberCommand(
    StaffType StaffType,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateOnly? HireDate,
    string? Specialty,
    string? ColorHex) : IRequest<ErrorOr<StaffMemberResponse>>;

