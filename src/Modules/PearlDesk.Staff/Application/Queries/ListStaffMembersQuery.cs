﻿using ErrorOr;
using MediatR;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Queries;

public record ListStaffMembersQuery(
    StaffType? StaffType = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IRequest<ErrorOr<PagedResult<StaffMemberResponse>>>;

