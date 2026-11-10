namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Whether a patient has anything the clinic must keep (people-merge plan D9):
/// appointments in any status, treatment plans, invoices or documents. For a patient whose
/// card was never opened, missed visits (cancelled, no-show) are not history: they never came.
/// </summary>
public interface IPatientHistoryReader
{
    Task<bool> HasHistoryAsync(Guid patientId, bool missedVisitsAreHistory, CancellationToken ct);

    /// <summary>Soft-deletes the patient's cancelled and no-show visits (deleting a patient who never came).</summary>
    Task RemoveMissedVisitsAsync(Guid patientId, CancellationToken ct);
}
