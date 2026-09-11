using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeShiftRepository : IShiftRepository
{
    private readonly Dictionary<ShiftId, Shift> _shifts = [];
    private int _nextShiftNumber = 1001;

    public Task<Shift?> GetByIdAsync(ShiftId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_shifts.GetValueOrDefault(id));

    public Task<Shift?> GetActiveShiftForTerminalAsync(TerminalId terminalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_shifts.Values.FirstOrDefault(s => s.TerminalId == terminalId && s.Status == ShiftStatus.Open));

    public Task<Shift?> GetActiveShiftForCashierAsync(UserId cashierId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_shifts.Values.FirstOrDefault(s => s.CashierId == cashierId && s.Status == ShiftStatus.Open));

    public Task<IReadOnlyList<Shift>> SearchShiftsAsync(
        TerminalId? terminalId = null,
        UserId? cashierId = null,
        ShiftStatus? status = null,
        DateTimeOffset? fromDateUtc = null,
        DateTimeOffset? toDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _shifts.Values.AsEnumerable();

        if (terminalId != null)
            query = query.Where(s => s.TerminalId == terminalId);

        if (cashierId != null)
            query = query.Where(s => s.CashierId == cashierId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (fromDateUtc.HasValue)
            query = query.Where(s => s.OpenedAtUtc >= fromDateUtc.Value);

        if (toDateUtc.HasValue)
            query = query.Where(s => s.OpenedAtUtc <= toDateUtc.Value);

        return Task.FromResult<IReadOnlyList<Shift>>(query.OrderByDescending(s => s.OpenedAtUtc).ToList());
    }

    public Task<int> GetNextShiftNumberAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_nextShiftNumber++);

    public Task AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        _shifts[shift.Id] = shift;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        _shifts[shift.Id] = shift;
        return Task.CompletedTask;
    }
}
