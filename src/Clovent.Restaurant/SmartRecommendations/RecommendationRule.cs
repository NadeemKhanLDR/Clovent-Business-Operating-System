using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Restaurant.SmartRecommendations;

/// <summary>
/// A back-office-configured upsell rule for the POS "smart suggestions"
/// panel: when the basket contains the trigger product (or, when
/// <see cref="ProductId"/> is <see langword="null"/>, any basket at all),
/// suggest <see cref="RecommendedVariantId"/> - subject to the rule's
/// local-time window and day-of-week mask. Deterministic configuration, not
/// machine learning: the Application layer orders matching rules by
/// <see cref="Priority"/> and only falls back to "popular today" heuristics
/// when too few rules match to fill the panel.
/// </summary>
public sealed class RecommendationRule : AggregateRoot<RecommendationRuleId>
{
    /// <summary>
    /// The catalog product that triggers this rule - <see langword="null"/>
    /// for an "any basket" rule that matches regardless of basket contents.
    /// </summary>
    public Guid? ProductId { get; private set; }

    /// <summary>The variant suggested when this rule matches.</summary>
    public ProductVariantId RecommendedVariantId { get; private set; }

    /// <summary>Lower values surface first on the suggestions panel.</summary>
    public int Priority { get; private set; }

    /// <summary>Whether this rule currently participates in suggestion selection.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Start of the rule's daily local-time window (e.g. 07:00), or
    /// <see langword="null"/> for no start constraint.
    /// </summary>
    public TimeSpan? StartTime { get; private set; }

    /// <summary>End of the rule's daily local-time window, or <see langword="null"/> for no end constraint.</summary>
    public TimeSpan? EndTime { get; private set; }

    /// <summary>
    /// Bitmask of days-of-week the rule applies on (bit 0 = Sunday .. bit 6 =
    /// Saturday), or <see langword="null"/> for "every day".
    /// </summary>
    public int? DaysOfWeek { get; private set; }

    /// <summary>Optional free-text notes (e.g. why this rule exists).</summary>
    public string? Notes { get; private set; }

    /// <summary>UTC instant this rule was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this rule was last changed.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private RecommendationRule(
        RecommendationRuleId id,
        Guid? productId,
        ProductVariantId recommendedVariantId,
        int priority,
        bool isActive,
        TimeSpan? startTime,
        TimeSpan? endTime,
        int? daysOfWeek,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        ProductId = productId;
        RecommendedVariantId = recommendedVariantId;
        Priority = priority;
        IsActive = isActive;
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeek = daysOfWeek;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Creates a new, active recommendation rule.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="priority"/> is negative, <paramref name="startTime"/> is not before <paramref name="endTime"/>, or <paramref name="daysOfWeek"/> is not a valid 7-bit mask.</exception>
    public static RecommendationRule Create(
        Guid? productId,
        ProductVariantId recommendedVariantId,
        int priority,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        int? daysOfWeek = null,
        string? notes = null)
    {
        RequireValidPriority(priority);
        RequireValidWindow(startTime, endTime);
        RequireValidDaysOfWeek(daysOfWeek);

        var now = DateTimeOffset.UtcNow;
        return new RecommendationRule(RecommendationRuleId.New(), productId, recommendedVariantId, priority, true, startTime, endTime, daysOfWeek, notes?.Trim(), now, now);
    }

    /// <summary>Updates the rule's configuration.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="priority"/> is negative, <paramref name="startTime"/> is not before <paramref name="endTime"/>, or <paramref name="daysOfWeek"/> is not a valid 7-bit mask.</exception>
    public void Update(
        Guid? productId,
        ProductVariantId recommendedVariantId,
        int priority,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        int? daysOfWeek = null,
        string? notes = null)
    {
        RequireValidPriority(priority);
        RequireValidWindow(startTime, endTime);
        RequireValidDaysOfWeek(daysOfWeek);

        ProductId = productId;
        RecommendedVariantId = recommendedVariantId;
        Priority = priority;
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeek = daysOfWeek;
        Notes = notes?.Trim();
        Touch();
    }

    /// <summary>Activates or deactivates the rule.</summary>
    public void SetStatus(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        Touch();
    }

    /// <summary>
    /// Whether this rule matches the given basket and local time: the trigger
    /// product is in the basket (or the rule triggers on any basket), the
    /// local time falls inside the window, and the local day-of-week bit is
    /// set (or no mask is configured).
    /// </summary>
    public bool Matches(IReadOnlyCollection<Guid> basketProductIds, TimeSpan localTimeOfDay, DayOfWeek localDayOfWeek)
    {
        if (!IsActive)
        {
            return false;
        }

        if (ProductId is not null && !basketProductIds.Contains(ProductId.Value))
        {
            return false;
        }

        if (StartTime is not null && localTimeOfDay < StartTime)
        {
            return false;
        }

        if (EndTime is not null && localTimeOfDay >= EndTime)
        {
            return false;
        }

        if (DaysOfWeek is not null && (DaysOfWeek.Value & (1 << (int)localDayOfWeek)) == 0)
        {
            return false;
        }

        return true;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static void RequireValidPriority(int priority)
    {
        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority cannot be negative.");
    }

    private static void RequireValidWindow(TimeSpan? startTime, TimeSpan? endTime)
    {
        if (startTime is not null && endTime is not null && startTime >= endTime)
            throw new ArgumentOutOfRangeException(nameof(endTime), endTime, "Start time must be before end time.");
    }

    private static void RequireValidDaysOfWeek(int? daysOfWeek)
    {
        if (daysOfWeek is not null and (< 0 or > 0x7F))
            throw new ArgumentOutOfRangeException(nameof(daysOfWeek), daysOfWeek, "Days-of-week must be a 7-bit mask (bit 0 = Sunday .. bit 6 = Saturday).");
    }
}
