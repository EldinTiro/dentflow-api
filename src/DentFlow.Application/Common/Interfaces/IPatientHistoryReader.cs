namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Whether a patient has anything the clinic must keep (people-merge plan D9):
/// appointments in any status, treatment plans, invoices or documents.
/// </summary>
public interface IPatientHistoryReader
{
    Task<bool> HasHistoryAsync(Guid patientId, CancellationToken ct);
}
