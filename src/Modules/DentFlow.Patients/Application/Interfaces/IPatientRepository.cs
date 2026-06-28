﻿using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Patient?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default);
    Task<Patient?> GetByPatientNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Patient> Items, int Total)> ListAsync(
        string? searchTerm,
        PatientStatus? status,
        string? recallFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<string> GeneratePatientNumberAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Patient patient, CancellationToken cancellationToken = default);

    // Emergency contacts
    Task<IReadOnlyList<PatientEmergencyContact>> ListEmergencyContactsAsync(Guid patientId, CancellationToken ct = default);
    Task<PatientEmergencyContact> AddEmergencyContactAsync(PatientEmergencyContact contact, CancellationToken ct = default);
    Task<PatientEmergencyContact?> GetEmergencyContactAsync(Guid patientId, Guid contactId, CancellationToken ct = default);
    Task DeleteEmergencyContactAsync(PatientEmergencyContact contact, CancellationToken ct = default);

    // Allergies
    Task<IReadOnlyList<Allergy>> ListAllergiesAsync(Guid patientId, CancellationToken ct = default);
    Task<Allergy> AddAllergyAsync(Allergy allergy, CancellationToken ct = default);
    Task<Allergy?> GetAllergyAsync(Guid patientId, Guid allergyId, CancellationToken ct = default);
    Task DeleteAllergyAsync(Allergy allergy, CancellationToken ct = default);

    // Medical history
    Task<MedicalHistory?> GetCurrentMedicalHistoryAsync(Guid patientId, CancellationToken ct = default);
    Task<IReadOnlyList<MedicalHistory>> GetCurrentMedicalHistoriesAsync(Guid patientId, CancellationToken ct = default);
    Task<MedicalHistory> AddMedicalHistoryAsync(MedicalHistory record, CancellationToken ct = default);
}

