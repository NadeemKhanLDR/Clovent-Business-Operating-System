namespace Clovent.Restaurant.SmartCombos;

/// <summary>Persisted manager decision, separate from recomputed analytics.</summary>
public sealed class ComboDecision
{
    public Guid WarehouseId { get; private set; }
    public string Signature { get; private set; } = "";
    public Guid? TemplateId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset DecidedAtUtc { get; private set; }
    public DateTimeOffset? DismissedUntilUtc { get; private set; }
    public string? Reason { get; private set; }
    public Guid Version { get; private set; }
    private ComboDecision() { }
    public static ComboDecision Create(Guid warehouseId, string signature) => new() { WarehouseId = warehouseId, Signature = signature };
    public void Dismiss(Guid userId, string? reason, int days)
    {
        if (TemplateId != null) throw new InvalidOperationException("This opportunity was already converted.");
        if (reason?.Length > 250) throw new ArgumentException("Reason must be at most 250 characters.");
        UserId = userId; Reason = reason; DecidedAtUtc = DateTimeOffset.UtcNow;
        DismissedUntilUtc = DecidedAtUtc.AddDays(days); Version = Guid.NewGuid();
    }
    public void Convert(Guid userId, Guid templateId)
    {
        if (TemplateId != null) throw new InvalidOperationException("This opportunity was already converted.");
        UserId = userId; TemplateId = templateId; DecidedAtUtc = DateTimeOffset.UtcNow;
        DismissedUntilUtc = null; Version = Guid.NewGuid();
    }
    public bool Suppresses(DateTimeOffset now) => TemplateId != null || DismissedUntilUtc > now;
}
