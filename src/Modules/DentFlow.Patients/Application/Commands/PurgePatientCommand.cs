using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

/// <summary>The Administrator deletes a patient for good, with everything on the record.</summary>
public record PurgePatientCommand(Guid PatientId) : IPatientScoped, IRequest<ErrorOr<Deleted>>;

/// <summary>
/// User request 2026-11-13. <see cref="IPatientScoped"/> has already answered 404 for a patient
/// of another clinic. The files go after the commit: a file left behind is harmless, a record
/// pointing at a missing file is not.
/// </summary>
public class PurgePatientCommandHandler(
    IPeoplePurger purger, ITransactionRunner transaction, IStorageService storage, ILogger<PurgePatientCommandHandler> logger)
    : IRequestHandler<PurgePatientCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(PurgePatientCommand command, CancellationToken cancellationToken)
    {
        var purged = await transaction.ExecuteAsync<IReadOnlyList<string>>(
            async ct => ErrorOrFactory.From(await purger.PurgePatientAsync(command.PatientId, ct)),
            cancellationToken);
        if (purged.IsError) return purged.Errors;

        foreach (var key in purged.Value)
        {
            try { await storage.DeleteAsync(key, cancellationToken); }
            catch (Exception ex) { logger.LogWarning(ex, "Purged patient {PatientId}: file {Key} could not be removed", command.PatientId, key); }
        }
        return Result.Deleted;
    }
}
