using ErrorOr;

namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Runs work in one database transaction: committed when the result is a value, rolled
/// back when it is an error or an exception escapes. Identity and the modules share one
/// DbContext, so this covers account and profile writes together (people-merge plan 2.1).
/// Nested calls join the outer transaction.
/// </summary>
public interface ITransactionRunner
{
    Task<ErrorOr<T>> ExecuteAsync<T>(Func<CancellationToken, Task<ErrorOr<T>>> work, CancellationToken ct);
}
