namespace Clovent.Desktop.Forms.Base.Appearance;

/// <summary>
/// One owner-configured styling rule - "Table Label: Segoe UI, 18, Bold,
/// Blue" - scoped to the whole system, one module, one screen, every
/// control of one type, or one specific named control (see
/// <see cref="AppearanceScopeType"/>). Every styling field is nullable/
/// optional: a rule only overrides what it explicitly sets, leaving
/// everything else at whatever a less-specific rule (or the control's own
/// default) already provides - "Font Family: Segoe UI" alone doesn't force
/// a size/color/weight on anything.
/// </summary>
public sealed record AppearanceRule(
    Guid RuleId,
    AppearanceScopeType ScopeType,
    string ScopeKey,
    string? FontFamily,
    float? FontSize,
    bool? Bold,
    bool? Italic,
    bool? Underline,
    bool? Strikeout,
    string? ForeColorHex,
    string? BackColorHex)
{
    /// <summary>A short, human-readable description of what this rule targets, for the Appearance grid.</summary>
    public string ScopeDescription => ScopeType switch
    {
        AppearanceScopeType.System => "Entire CBOS",
        AppearanceScopeType.Module => $"Module: {ScopeKey}",
        AppearanceScopeType.Form => $"Screen: {ScopeKey}",
        AppearanceScopeType.ControlType => $"Control type: {ScopeKey}",
        AppearanceScopeType.Control => $"Control: {ScopeKey}",
        _ => ScopeKey,
    };
}
