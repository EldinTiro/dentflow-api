using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Queries;

/// <param name="AccessDetails">
/// Role and last sign-in of a linked account; only for people.manage (DT-34). Others see
/// whether the person can sign in, nothing more.
/// </param>
public record GetStaffMemberByIdQuery(Guid Id, bool AccessDetails = true) : IRequest<ErrorOr<StaffMemberResponse>>;

