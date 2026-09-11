using System;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.Shifts.Dtos;

/// <summary>Read-model shape for a <see cref="CashMovement"/> entity.</summary>
public sealed record CashMovementDto(
    Guid CashMovementId,
    Guid ShiftId,
    string Type,
    decimal Amount,
    string Reason,
    Guid UserId,
    DateTimeOffset TimestampUtc,
    string? Notes)
{
    /// <summary>Projects a domain <see cref="CashMovement"/> into its DTO.</summary>
    public static CashMovementDto FromDomain(CashMovement movement) => new(
        movement.Id.Value,
        movement.ShiftId.Value,
        movement.Type.ToString(),
        movement.Amount,
        movement.Reason,
        movement.UserId.Value,
        movement.TimestampUtc,
        movement.Notes);
}
