using Clovent.Restaurant.Continuity;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeContinuityJournalStore : IContinuityJournalStore
{
    private readonly List<EmergencyTransaction> _transactions = [];
    private long _nextSequence = 1;

    public Task AppendAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default)
    {
        _transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmergencyTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EmergencyTransaction>>(_transactions.AsReadOnly());
    }

    public Task<IReadOnlyList<EmergencyTransaction>> GetPendingReplayAsync(CancellationToken cancellationToken = default)
    {
        var pending = _transactions
            .Where(t => t.ReconciliationStatus == ReconciliationStatus.PendingReplay)
            .OrderBy(t => t.SequenceNumber)
            .ToList();
        return Task.FromResult<IReadOnlyList<EmergencyTransaction>>(pending.AsReadOnly());
    }

    public Task UpdateAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default)
    {
        var index = _transactions.FindIndex(t => t.TransactionId == transaction.TransactionId);
        if (index >= 0)
        {
            _transactions[index] = transaction;
        }
        else
        {
            _transactions.Add(transaction);
        }
        return Task.CompletedTask;
    }

    public Task<ContinuityJournalStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var total = _transactions.Count;
        var pending = _transactions.Count(t => t.ReconciliationStatus == ReconciliationStatus.PendingReplay);
        var replayed = _transactions.Count(t => t.ReconciliationStatus == ReconciliationStatus.Replayed);
        var conflict = _transactions.Count(t => t.ReconciliationStatus is ReconciliationStatus.Conflict or ReconciliationStatus.Failed);
        var oldest = _transactions
            .Where(t => t.ReconciliationStatus == ReconciliationStatus.PendingReplay)
            .Select(t => (DateTimeOffset?)t.TimestampUtc)
            .FirstOrDefault();

        return Task.FromResult(new ContinuityJournalStatistics(total, pending, replayed, conflict, oldest));
    }

    public Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_nextSequence++);
    }

    public Task<string> GetLastTransactionHashAsync(CancellationToken cancellationToken = default)
    {
        var last = _transactions.LastOrDefault();
        var hash = !string.IsNullOrEmpty(last?.HmacSignature) ? last.HmacSignature : (!string.IsNullOrEmpty(last?.Checksum) ? last.Checksum : EmergencyTransaction.GenesisHash);
        return Task.FromResult(hash);
    }
}
