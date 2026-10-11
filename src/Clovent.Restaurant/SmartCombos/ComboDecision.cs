namespace Clovent.Restaurant.SmartCombos;

/// <summary>Persisted manager decision, separate from recomputed analytics.</summary>
public sealed class ComboDecision
{
    /// <summary>Warehouse identifier for the decision scope.</summary>
    public Guid WarehouseId { get; private set; }

    /// <summary>Canonical item-combination signature.</summary>
    public string Signature { get; private set; } = "";

    /// <summary>Converted quick-order template identifier, if converted.</summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>Identifier of the manager who recorded the decision.</summary>
    public Guid UserId { get; private set; }

    /// <summary>UTC timestamp when the decision was recorded.</summary>
    public DateTimeOffset DecidedAtUtc { get; private set; }

    /// <summary>UTC timestamp until which the combo suggestion remains dismissed.</summary>
    public DateTimeOffset? DismissedUntilUtc { get; private set; }

    /// <summary>Optional dismissal reason recorded by the manager.</summary>
    public string? Reason { get; private set; }

    /// <summary>Concurrency version token.</summary>
    public Guid Version { get; private set; }

    private ComboDecision() { }

    /// <summary>Creates a new <see cref="ComboDecision"/> for the specified warehouse and combo signature.</summary>
    public static ComboDecision Create(Guid warehouseId, string signature) => new() { WarehouseId = warehouseId, Signature = signature };

    /// <summary>Dismisses the combo opportunity for the specified number of days.</summary>
    public void Dismiss(Guid userId, string? reason, int days)
    {
        if (TemplateId != null) throw new InvalidOperationException("This opportunity was already converted.");
        if (reason?.Length > 250) throw new ArgumentException("Reason must be at most 250 characters.");
        UserId = userId; Reason = reason; DecidedAtUtc = DateTimeOffset.UtcNow;
        DismissedUntilUtc = DecidedAtUtc.AddDays(days); Version = Guid.NewGuid();
    }

    /// <summary>Marks the combo opportunity as converted into a quick-order template.</summary>
    public void Convert(Guid userId, Guid templateId)
    {
        if (TemplateId != null) throw new InvalidOperationException("This opportunity was already converted.");
        UserId = userId; TemplateId = templateId; DecidedAtUtc = DateTimeOffset.UtcNow;
        DismissedUntilUtc = null; Version = Guid.NewGuid();
    }

    /// <summary>Returns true when this decision currently suppresses the combo suggestion.</summary>
    public bool Suppresses(DateTimeOffset now) => TemplateId != null || DismissedUntilUtc > now;
}
