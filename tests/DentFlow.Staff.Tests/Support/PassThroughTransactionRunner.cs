using ErrorOr;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Staff.Tests.Support;

/// <summary>Runs the work directly; rollback itself is covered by TransactionRunnerTests.</summary>
public sealed class PassThroughTransactionRunner : ITransactionRunner
{
    public int Calls { get; private set; }

    public Task<ErrorOr<T>> ExecuteAsync<T>(Func<CancellationToken, Task<ErrorOr<T>>> work, CancellationToken ct)
    {
        Calls++;
        return work(ct);
    }
}
