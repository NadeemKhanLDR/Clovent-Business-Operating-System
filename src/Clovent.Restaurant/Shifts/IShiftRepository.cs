using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;

namespace Clovent.Restaurant.Shifts;

/// <summary>Repository contract for managing <see cref="Shift"/> aggregates.</summary>
public interface IShiftRepository
{
    /// <summary>Gets a shift by its unique identifier.</summary>
    Task<Shift?> GetByIdAsync(ShiftId id, CancellationToken cancellationToken = default);

    /// <summary>Gets the currently open shift for a specific terminal, or null if none is open.</summary>
    Task<Shift?> GetActiveShiftForTerminalAsync(TerminalId terminalId, CancellationToken cancellationToken = default);

    /// <summary>Gets the currently open shift for a specific cashier, or null if none is open.</summary>
    Task<Shift?> GetActiveShiftForCashierAsync(UserId cashierId, CancellationToken cancellationToken = default);

    /// <summary>Searches historical and active shifts using filter criteria.</summary>
    Task<IReadOnlyList<Shift>> SearchShiftsAsync(
        TerminalId? terminalId = null,
        UserId? cashierId = null,
        ShiftStatus? status = null,
        DateTimeOffset? fromDateUtc = null,
        DateTimeOffset? toDateUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the next sequential shift number.</summary>
    Task<int> GetNextShiftNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new shift aggregate to persistence.</summary>
    Task AddAsync(Shift shift, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing shift aggregate in persistence.</summary>
    Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default);
}
