﻿using ErrorOr;
using MediatR;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Queries;

public record ListStaffMembersQuery(
    StaffType? StaffType = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null) : IRequest<ErrorOr<PagedResult<StaffMemberResponse>>>;

