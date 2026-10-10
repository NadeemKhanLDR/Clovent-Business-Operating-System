using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Sync;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Sync;

/// <summary>
/// Production durable implementation of <see cref="IShiftSyncRegistry"/> backed by SQL Server EF Core.
/// Replicates cashier shift drawer opening, closing, and cash variance data durably across the branch.
/// </summary>
public sealed class DurableShiftSyncRegistry : IShiftSyncRegistry
{
    private readonly RestaurantDbContext _dbContext;
    private readonly ILogger<DurableShiftSyncRegistry> _logger;

    /// <summary>Creates a new durable shift sync registry.</summary>
    public DurableShiftSyncRegistry(
        RestaurantDbContext dbContext,
        ILogger<DurableShiftSyncRegistry> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task RecordShiftSummaryAsync(ShiftSummaryDeltaSyncPayload summary, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var existing = await _dbContext.TerminalShiftSyncSummaries
            .FirstOrDefaultAsync(s => s.BranchId == summary.BranchId &&
                                      s.TerminalId == summary.TerminalId &&
                                      s.ShiftNumber == summary.ShiftNumber, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null)
        {
            var entity = new TerminalShiftSyncSummary
            {
                Id = Guid.NewGuid(),
                BranchId = summary.BranchId,
                TerminalId = summary.TerminalId,
                ShiftNumber = summary.ShiftNumber,
                CashierId = summary.CashierId,
                CashierName = summary.CashierName,
                OpenedAtUtc = summary.OpenedAtUtc,
                ClosedAtUtc = summary.ClosedAtUtc,
                StartingCash = summary.StartingCash,
                CountedCash = summary.CountedCash,
                ExpectedCash = summary.ExpectedCash,
                CashVariance = summary.CashVariance,
                NetSales = summary.NetSales,
                TotalOrdersCount = summary.TotalOrdersCount,
                TimestampUtc = summary.TimestampUtc,
                ReplicatedAtUtc = DateTimeOffset.UtcNow
            };
            await _dbContext.TerminalShiftSyncSummaries.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.CashierId = summary.CashierId;
            existing.CashierName = summary.CashierName;
            existing.ClosedAtUtc = summary.ClosedAtUtc;
            existing.CountedCash = summary.CountedCash;
            existing.ExpectedCash = summary.ExpectedCash;
            existing.CashVariance = summary.CashVariance;
            existing.NetSales = summary.NetSales;
            existing.TotalOrdersCount = summary.TotalOrdersCount;
            existing.TimestampUtc = summary.TimestampUtc;
            existing.ReplicatedAtUtc = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ShiftSummaryDeltaSyncPayload>> GetBranchShiftSummariesAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.TerminalShiftSyncSummaries
            .AsNoTracking()
            .Where(s => s.BranchId == branchId)
            .OrderByDescending(s => s.OpenedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(s => new ShiftSummaryDeltaSyncPayload(
            TerminalId: s.TerminalId,
            BranchId: s.BranchId,
            ShiftNumber: s.ShiftNumber,
            CashierId: s.CashierId,
            CashierName: s.CashierName,
            OpenedAtUtc: s.OpenedAtUtc,
            ClosedAtUtc: s.ClosedAtUtc,
            StartingCash: s.StartingCash,
            CountedCash: s.CountedCash,
            ExpectedCash: s.ExpectedCash,
            CashVariance: s.CashVariance,
            NetSales: s.NetSales,
            TotalOrdersCount: s.TotalOrdersCount,
            TimestampUtc: s.TimestampUtc)).ToList();
    }

    /// <inheritdoc/>
    public async Task<ShiftSummaryDeltaSyncPayload?> GetLatestTerminalShiftAsync(Guid branchId, Guid terminalId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TerminalShiftSyncSummaries
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.TerminalId == terminalId)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity == null) return null;

        return new ShiftSummaryDeltaSyncPayload(
            TerminalId: entity.TerminalId,
            BranchId: entity.BranchId,
            ShiftNumber: entity.ShiftNumber,
            CashierId: entity.CashierId,
            CashierName: entity.CashierName,
            OpenedAtUtc: entity.OpenedAtUtc,
            ClosedAtUtc: entity.ClosedAtUtc,
            StartingCash: entity.StartingCash,
            CountedCash: entity.CountedCash,
            ExpectedCash: entity.ExpectedCash,
            CashVariance: entity.CashVariance,
            NetSales: entity.NetSales,
            TotalOrdersCount: entity.TotalOrdersCount,
            TimestampUtc: entity.TimestampUtc);
    }
}
