using System;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;

namespace Clovent.Restaurant.Shifts.Events;

/// <summary>Raised when a new <see cref="Shift"/> register session is opened.</summary>
public sealed record ShiftOpened(
    ShiftId ShiftId,
    int ShiftNumber,
    BranchId BranchId,
    WarehouseId WarehouseId,
    TerminalId TerminalId,
    UserId CashierId,
    decimal StartingCash,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
