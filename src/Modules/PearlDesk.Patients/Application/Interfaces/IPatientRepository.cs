﻿using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Patient?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Patient> Items, int Total)> ListAsync(
        string? searchTerm,
        PatientStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<string> GeneratePatientNumberAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Patient patient, CancellationToken cancellationToken = default);
}

