using Clovent.Domain;
using Clovent.Restaurant.ActivityLogs.Events;

namespace Clovent.Restaurant.ActivityLogs;

/// <summary>
/// One recorded action for the Restaurant POS activity log - "who did what,
/// when, and where" for actions worth an audit trail (New Order, Remove
/// Line, Price Override, Discount, Payment, Refund, Print, Setup Changes,
/// Login/Logout - see <c>docs</c> for the full brief). Immutable once
/// created, the same "applied instance, not an editable record" shape
/// <see cref="Discounts.Discount"/>/<see cref="ServiceCharges.ServiceCharge"/>
/// already establish - a log entry is never corrected in place, only ever
/// appended to.
/// </summary>
public sealed class ActivityLogEntry : AggregateRoot<ActivityLogEntryId>
{
    private const int MaxActionLength = 100;
    private const int MaxDetailsLength = 1000;
    private const int MaxPerformedByLength = 200;
    private const int MaxMachineNameLength = 100;

    /// <summary>The short action name - "New Order", "Remove Line", "Price Override", "Discount", "Payment", "Refund", "Print", "Setup Changes", "Login", "Logout".</summary>
    public string Action { get; }

    /// <summary>Optional free-text detail (e.g. an order number, a reason, an amount) - whatever makes this entry meaningful on its own when read later.</summary>
    public string? Details { get; }

    /// <summary>Who performed the action - a display name/username, not a foreign key (this aggregate has no notion of "current user" beyond what the caller supplies, the same pattern <see cref="OrderLines.OrderLine.OverridePrice"/> already uses).</summary>
    public string PerformedBy { get; }

    /// <summary>The workstation the action was performed from.</summary>
    public string MachineName { get; }

    /// <summary>UTC instant this action occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ActivityLogEntry(ActivityLogEntryId id, string action, string? details, string performedBy, string machineName, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        Action = action;
        Details = details;
        PerformedBy = performedBy;
        MachineName = machineName;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Records a new activity log entry.</summary>
    /// <exception cref="ArgumentException"><paramref name="action"/> or <paramref name="performedBy"/> is empty, or any field exceeds its maximum length.</exception>
    public static ActivityLogEntry Record(string action, string? details, string performedBy, string machineName)
    {
        action = RequireField(action, nameof(action), MaxActionLength);
        performedBy = RequireField(performedBy, nameof(performedBy), MaxPerformedByLength);
        machineName = RequireField(machineName, nameof(machineName), MaxMachineNameLength);

        details = details?.Trim();
        if (details is { Length: > MaxDetailsLength })
        {
            details = details[..MaxDetailsLength];
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new ActivityLogEntry(ActivityLogEntryId.New(), action, string.IsNullOrEmpty(details) ? null : details, performedBy, machineName, now);
        entry.AddDomainEvent(new ActivityLogEntryRecorded(entry.Id, entry.Action, now));
        return entry;
    }

    private static string RequireField(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", fieldName);

        value = value.Trim();

        if (value.Length > maxLength)
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.", fieldName);

        return value;
    }
}
