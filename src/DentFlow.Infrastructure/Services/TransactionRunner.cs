using ErrorOr;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

internal sealed class TransactionRunner(ApplicationDbContext db) : ITransactionRunner
{
    public async Task<ErrorOr<T>> ExecuteAsync<T>(Func<CancellationToken, Task<ErrorOr<T>>> work, CancellationToken ct)
    {
        // Already inside a transaction: the outer call decides commit or rollback.
        if (db.Database.CurrentTransaction is not null)
            return await work(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        ErrorOr<T> result;
        try
        {
            result = await work(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }

        if (result.IsError)
        {
            await tx.RollbackAsync(CancellationToken.None);
            // Nothing tracked may be saved later by accident.
            db.ChangeTracker.Clear();
            return result;
        }

        await tx.CommitAsync(ct);
        return result;
    }
}
