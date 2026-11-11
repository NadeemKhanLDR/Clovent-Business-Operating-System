namespace Clovent.Restaurant.Application.Concurrency;

/// <summary>Persistence-wide coordination held through the command's commit boundary.</summary>
public interface ITransactionLockProvider
{
    /// <summary>Executes an operation while holding the supplied canonical aggregate locks.</summary>
    Task<T> ExecuteAsync<T>(IReadOnlyList<TransactionLockKey> keys, CancellationToken cancellationToken, Func<Task<T>> action);
}
