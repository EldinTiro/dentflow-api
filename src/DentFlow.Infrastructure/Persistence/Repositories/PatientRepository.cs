﻿using Microsoft.EntityFrameworkCore;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Infrastructure.Persistence.Repositories;

public class PatientRepository(ApplicationDbContext dbContext) : IPatientRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Patient>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Patient?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await dbContext.Set<Patient>().FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<Patient?> GetByPatientNumberAsync(string patientNumber, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Patient>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PatientNumber == patientNumber, cancellationToken);

    public async Task<(IReadOnlyList<Patient> Items, int Total)> ListAsync(
        string? searchTerm, PatientStatus? status, string? recallFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Patient>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                (p.Email != null && p.Email.ToLower().Contains(term)) ||
                p.PatientNumber.ToLower().Contains(term));
        }

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(recallFilter))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var thirtyDaysLater = today.AddDays(30);
            query = recallFilter switch
            {
                "overdue"  => query.Where(p => p.RecallDueDate != null && p.RecallDueDate < today),
                "due-soon" => query.Where(p => p.RecallDueDate != null && p.RecallDueDate >= today && p.RecallDueDate <= thirtyDaysLater),
                _          => query,
            };
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<string> GeneratePatientNumberAsync(CancellationToken cancellationToken = default)
    {
        var count = await dbContext.Set<Patient>().IgnoreQueryFilters()
            .CountAsync(p => p.TenantId == dbContext.Set<Patient>().Select(x => x.TenantId).FirstOrDefault(), cancellationToken);
        return $"P-{(count + 1):D6}";
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Patient>().AddAsync(patient, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Patient>().Update(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        patient.SoftDelete();
        dbContext.Set<Patient>().Update(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientEmergencyContact>> ListEmergencyContactsAsync(Guid patientId, CancellationToken ct = default) =>
        await dbContext.PatientEmergencyContacts
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<PatientEmergencyContact> AddEmergencyContactAsync(PatientEmergencyContact contact, CancellationToken ct = default)
    {
        dbContext.PatientEmergencyContacts.Add(contact);
        await dbContext.SaveChangesAsync(ct);
        return contact;
    }

    public async Task<PatientEmergencyContact?> GetEmergencyContactAsync(Guid patientId, Guid contactId, CancellationToken ct = default) =>
        await dbContext.PatientEmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.PatientId == patientId, ct);

    public async Task DeleteEmergencyContactAsync(PatientEmergencyContact contact, CancellationToken ct = default)
    {
        dbContext.PatientEmergencyContacts.Remove(contact);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Allergy>> ListAllergiesAsync(Guid patientId, CancellationToken ct = default) =>
        await dbContext.Allergies
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.Allergen)
            .ToListAsync(ct);

    public async Task<Allergy> AddAllergyAsync(Allergy allergy, CancellationToken ct = default)
    {
        dbContext.Allergies.Add(allergy);
        await dbContext.SaveChangesAsync(ct);
        return allergy;
    }

    public async Task<Allergy?> GetAllergyAsync(Guid patientId, Guid allergyId, CancellationToken ct = default) =>
        await dbContext.Allergies
            .FirstOrDefaultAsync(a => a.Id == allergyId && a.PatientId == patientId, ct);

    public async Task DeleteAllergyAsync(Allergy allergy, CancellationToken ct = default)
    {
        dbContext.Allergies.Remove(allergy);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<MedicalHistory?> GetCurrentMedicalHistoryAsync(Guid patientId, CancellationToken ct = default) =>
        await dbContext.MedicalHistories
            .Where(m => m.PatientId == patientId && m.IsCurrent)
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<MedicalHistory>> GetCurrentMedicalHistoriesAsync(Guid patientId, CancellationToken ct = default) =>
        await dbContext.MedicalHistories
            .Where(m => m.PatientId == patientId && m.IsCurrent)
            .ToListAsync(ct);

    public async Task<MedicalHistory> AddMedicalHistoryAsync(MedicalHistory record, CancellationToken ct = default)
    {
        dbContext.MedicalHistories.Add(record);
        await dbContext.SaveChangesAsync(ct);
        return record;
    }
}

