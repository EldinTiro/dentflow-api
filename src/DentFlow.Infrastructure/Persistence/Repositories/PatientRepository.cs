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
            query = ApplySearch(query, searchTerm);

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

    /// <summary>
    /// QA-015. A query without letters is a phone number (or patient number): digits
    /// are compared ignoring "+", spaces and a leading trunk 0, so "+38761542",
    /// "061542" and "61 542" all find +38761542123. Otherwise every word must match a
    /// name, the email or the patient number — "Selma Aganović" narrows rather than
    /// widens — and names compare with Bosnian diacritics folded (č ć → c, š → s,
    /// ž → z, đ/dj → d), because people search for "aganovic".
    /// </summary>
    private static IQueryable<Patient> ApplySearch(IQueryable<Patient> query, string searchTerm)
    {
        var raw = searchTerm.Trim();

        if (!raw.Any(char.IsLetter))
        {
            var digits = new string(raw.Where(char.IsDigit).ToArray()).TrimStart('0');
            if (digits.Length == 0) return query.Where(p => p.PatientNumber.Contains(raw));

            return query.Where(p =>
                p.PatientNumber.Contains(raw) ||
                (p.PhoneMobile != null && p.PhoneMobile.Replace("+", "").Replace(" ", "").Contains(digits)) ||
                (p.PhoneHome != null && p.PhoneHome.Replace("+", "").Replace(" ", "").Contains(digits)) ||
                (p.PhoneWork != null && p.PhoneWork.Replace("+", "").Replace(" ", "").Contains(digits)));
        }

        foreach (var word in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var plain = Fold(word);
            var lower = word.ToLowerInvariant();
            query = query.Where(p =>
                p.FirstName.ToLower().Replace("dj", "d").Replace("č", "c").Replace("ć", "c").Replace("š", "s").Replace("ž", "z").Replace("đ", "d").Contains(plain) ||
                p.LastName.ToLower().Replace("dj", "d").Replace("č", "c").Replace("ć", "c").Replace("š", "s").Replace("ž", "z").Replace("đ", "d").Contains(plain) ||
                (p.PreferredName != null && p.PreferredName.ToLower().Replace("dj", "d").Replace("č", "c").Replace("ć", "c").Replace("š", "s").Replace("ž", "z").Replace("đ", "d").Contains(plain)) ||
                (p.Email != null && p.Email.ToLower().Contains(lower)) ||
                p.PatientNumber.ToLower().Contains(lower));
        }

        return query;
    }

    private static string Fold(string word) =>
        word.ToLowerInvariant()
            .Replace("dj", "d")
            .Replace('č', 'c').Replace('ć', 'c').Replace('š', 's').Replace('ž', 'z').Replace('đ', 'd');

    public async Task<string> GeneratePatientNumberAsync(CancellationToken cancellationToken = default)
    {
        // Scoped to the resolved tenant and counting soft-deleted rows too, so a
        // number is never reused within a clinic. IgnoreQueryFilters applies to the
        // whole query tree, so the tenant has to be supplied explicitly — the previous
        // version compared against an arbitrary row's TenantId and produced duplicates
        // as soon as more than one tenant existed.
        var tenantId = dbContext.ResolvedTenantId
            ?? throw new InvalidOperationException(
                "Cannot generate a patient number without a resolved tenant.");

        var count = await dbContext.Set<Patient>().IgnoreQueryFilters()
            .CountAsync(p => p.TenantId == tenantId, cancellationToken);

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
        // Soft delete: clinical data stays recoverable (people-merge plan D13).
        contact.SoftDelete();
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
        // Soft delete: clinical data stays recoverable (people-merge plan D13).
        allergy.SoftDelete();
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

