﻿using Microsoft.EntityFrameworkCore;
using PearlDesk.Infrastructure.Persistence;
using PearlDesk.Patients.Application.Interfaces;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Infrastructure.Persistence.Repositories;

public class PatientRepository(ApplicationDbContext dbContext) : IPatientRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Patient>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Patient?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await dbContext.Set<Patient>().FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<(IReadOnlyList<Patient> Items, int Total)> ListAsync(
        string? searchTerm, PatientStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
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
}

