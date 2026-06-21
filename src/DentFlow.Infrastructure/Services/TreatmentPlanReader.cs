using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Services;

public class TreatmentPlanReader(ApplicationDbContext db) : ITreatmentPlanReader
{
    public async Task<(Guid PatientId, IReadOnlyList<TreatmentPlanLineItem> Items)?> GetPlanItemsAsync(
        Guid treatmentPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await db.TreatmentPlans
            .FirstOrDefaultAsync(p => p.Id == treatmentPlanId, cancellationToken);

        if (plan is null) return null;

        var items = await db.TreatmentPlanItems
            .Where(i => i.TreatmentPlanId == treatmentPlanId)
            .Select(i => new TreatmentPlanLineItem(i.Id, i.Description, i.CdtCode, i.ToothNumber, i.Fee))
            .ToListAsync(cancellationToken);

        return (plan.PatientId, (IReadOnlyList<TreatmentPlanLineItem>)items.AsReadOnly());
    }
}
