using System.Collections.Concurrent;
using Clovent.Restaurant.Application.Outbox.Dtos;

namespace Clovent.Restaurant.Application.Sync;

/// <summary>
/// Registry storing terminal shift summaries replicated across the branch.
/// </summary>
public interface IShiftSyncRegistry
{
    /// <summary>Records or updates a terminal shift summary.</summary>
    Task RecordShiftSummaryAsync(ShiftSummaryDeltaSyncPayload summary, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all recorded shift summaries for a branch.</summary>
    Task<IReadOnlyList<ShiftSummaryDeltaSyncPayload>> GetBranchShiftSummariesAsync(Guid branchId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the latest recorded shift summary for a terminal.</summary>
    Task<ShiftSummaryDeltaSyncPayload?> GetLatestTerminalShiftAsync(Guid branchId, Guid terminalId, CancellationToken cancellationToken = default);
}

/// <summary>Thread-safe in-memory implementation of <see cref="IShiftSyncRegistry"/>.</summary>
public sealed class InMemoryShiftSyncRegistry : IShiftSyncRegistry
{
    private readonly ConcurrentDictionary<string, ShiftSummaryDeltaSyncPayload> _shifts = new();

    /// <inheritdoc/>
    public Task RecordShiftSummaryAsync(ShiftSummaryDeltaSyncPayload summary, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var key = $"{summary.BranchId}:{summary.TerminalId}:{summary.ShiftNumber}";
        _shifts[key] = summary;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ShiftSummaryDeltaSyncPayload>> GetBranchShiftSummariesAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var list = _shifts.Values
            .Where(s => s.BranchId == branchId)
            .OrderByDescending(s => s.OpenedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<ShiftSummaryDeltaSyncPayload>>(list);
    }

    /// <inheritdoc/>
    public Task<ShiftSummaryDeltaSyncPayload?> GetLatestTerminalShiftAsync(Guid branchId, Guid terminalId, CancellationToken cancellationToken = default)
    {
        var latest = _shifts.Values
            .Where(s => s.BranchId == branchId && s.TerminalId == terminalId)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }
}
