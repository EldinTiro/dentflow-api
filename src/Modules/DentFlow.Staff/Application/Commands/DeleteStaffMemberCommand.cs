using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Staff.Application.Commands;

public record DeleteStaffMemberCommand(Guid Id, AccountActor Actor) : IRequest<ErrorOr<Deleted>>;
