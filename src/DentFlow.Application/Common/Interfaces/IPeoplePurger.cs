namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Deletes a person for good, across modules (user request 2026-11-13). The one exception to
/// soft deletes: the Administrator's choice, behind <c>records.purge</c>. Callers check that the
/// person exists in their clinic first, and run this inside <see cref="ITransactionRunner"/>.
/// </summary>
public interface IPeoplePurger
{
    /// <summary>
    /// The patient and everything on their record: visits and their history, invoices with
    /// items and payments, plans, tooth chart, allergies, medical history, contacts, SMS log,
    /// files. Returns the storage keys of the files, to remove once the transaction commits.
    /// </summary>
    Task<IReadOnlyList<string>> PurgePatientAsync(Guid patientId, CancellationToken ct);

    /// <summary>
    /// A staff profile with no appointments, its hours and absences, and its sign-in account.
    /// Patients who preferred them and medical entries they recorded keep their data, unlinked.
    /// </summary>
    Task PurgeStaffMemberAsync(Guid staffMemberId, Guid? userId, CancellationToken ct);
}
