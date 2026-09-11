using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Shifts;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IShiftRepository"/>.</summary>
public sealed class ShiftRepository(RestaurantDbContext dbContext) : IShiftRepository
{
    /// <inheritdoc/>
    public async Task<Shift?> GetByIdAsync(ShiftId id, CancellationToken cancellationToken = default) =>
        await dbContext.Shifts
            .Include(s => s.CashMovements)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Shift?> GetActiveShiftForTerminalAsync(TerminalId terminalId, CancellationToken cancellationToken = default) =>
        await dbContext.Shifts
            .Include(s => s.CashMovements)
            .FirstOrDefaultAsync(s => s.TerminalId == terminalId && s.Status == ShiftStatus.Open, cancellationToken);

    /// <inheritdoc/>
    public async Task<Shift?> GetActiveShiftForCashierAsync(UserId cashierId, CancellationToken cancellationToken = default) =>
        await dbContext.Shifts
            .Include(s => s.CashMovements)
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.Status == ShiftStatus.Open, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Shift>> SearchShiftsAsync(
        TerminalId? terminalId = null,
        UserId? cashierId = null,
        ShiftStatus? status = null,
        DateTimeOffset? fromDateUtc = null,
        DateTimeOffset? toDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Shifts.Include(s => s.CashMovements).AsQueryable();

        if (terminalId.HasValue)
        {
            query = query.Where(s => s.TerminalId == terminalId.Value);
        }

        if (cashierId.HasValue)
        {
            query = query.Where(s => s.CashierId == cashierId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (fromDateUtc.HasValue)
        {
            query = query.Where(s => s.OpenedAtUtc >= fromDateUtc.Value);
        }

        if (toDateUtc.HasValue)
        {
            query = query.Where(s => s.OpenedAtUtc <= toDateUtc.Value);
        }

        return await query.OrderByDescending(s => s.ShiftNumber).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetNextShiftNumberAsync(CancellationToken cancellationToken = default)
    {
        var maxNumber = await dbContext.Shifts.MaxAsync(s => (int?)s.ShiftNumber, cancellationToken);
        return (maxNumber ?? 1000) + 1;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Shift shift, CancellationToken cancellationToken = default) =>
        await dbContext.Shifts.AddAsync(shift, cancellationToken);

    /// <inheritdoc/>
    public Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        dbContext.Shifts.Update(shift);
        return Task.CompletedTask;
    }
}
