using System;
using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Restaurant.Shifts;

/// <summary>
/// A cash deposit or withdrawal recorded against a <see cref="Shift"/> cash register session.
/// </summary>
public sealed class CashMovement : Entity<CashMovementId>
{
    /// <summary>The shift session this movement belongs to.</summary>
    public ShiftId ShiftId { get; }

    /// <summary>Whether cash was added (CashIn) or removed (CashOut).</summary>
    public CashMovementType Type { get; }

    /// <summary>The monetary amount of the movement.</summary>
    public decimal Amount { get; }

    /// <summary>The business reason for the cash movement.</summary>
    public string Reason { get; }

    /// <summary>The user who executed or authorized the cash movement.</summary>
    public UserId UserId { get; }

    /// <summary>UTC timestamp when the movement was recorded.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>Optional additional notes.</summary>
    public string? Notes { get; }

    /// <summary>Constructor for EF Core persistence.</summary>
    private CashMovement(
        CashMovementId id,
        ShiftId shiftId,
        CashMovementType type,
        decimal amount,
        string reason,
        UserId userId,
        DateTimeOffset timestampUtc,
        string? notes)
    {
        Id = id;
        ShiftId = shiftId;
        Type = type;
        Amount = amount;
        Reason = reason;
        UserId = userId;
        TimestampUtc = timestampUtc;
        Notes = notes;
    }

    /// <summary>Creates a new cash movement for an open shift.</summary>
    public static CashMovement Create(
        ShiftId shiftId,
        CashMovementType type,
        decimal amount,
        string reason,
        UserId userId,
        string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cash movement amount must be positive.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required for cash movement.", nameof(reason));

        return new CashMovement(
            CashMovementId.New(),
            shiftId,
            type,
            amount,
            reason.Trim(),
            userId,
            DateTimeOffset.UtcNow,
            notes?.Trim());
    }
}
