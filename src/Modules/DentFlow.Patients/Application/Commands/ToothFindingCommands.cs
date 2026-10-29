using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public record AddToothFindingCommand(
    Guid PatientId,
    int Tooth,
    string Condition,
    string? Surfaces,
    string? Notes) : IPatientScoped, IRequest<ErrorOr<ToothFindingResponse>>;

public class AddToothFindingCommandHandler(IPatientRepository repo)
    : IRequestHandler<AddToothFindingCommand, ErrorOr<ToothFindingResponse>>
{
    public async Task<ErrorOr<ToothFindingResponse>> Handle(AddToothFindingCommand cmd, CancellationToken ct)
    {
        var finding = ToothFinding.Create(cmd.PatientId, cmd.Tooth, cmd.Condition, cmd.Surfaces, cmd.Notes);
        if (finding.IsError) return finding.Errors;
        await repo.AddToothFindingAsync(finding.Value, ct);
        return ToothFindingResponse.FromEntity(finding.Value);
    }
}

public record DeleteToothFindingCommand(Guid PatientId, Guid FindingId)
    : IPatientScoped, IRequest<ErrorOr<Deleted>>;

public class DeleteToothFindingCommandHandler(IPatientRepository repo)
    : IRequestHandler<DeleteToothFindingCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteToothFindingCommand cmd, CancellationToken ct)
    {
        var finding = await repo.GetToothFindingAsync(cmd.PatientId, cmd.FindingId, ct);
        if (finding is null) return ToothFindingErrors.NotFound;
        await repo.DeleteToothFindingAsync(finding, ct);
        return Result.Deleted;
    }
}
