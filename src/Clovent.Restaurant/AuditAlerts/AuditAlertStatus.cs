namespace Clovent.Restaurant.AuditAlerts;

/// <summary>
/// Lifecycle status of an audit alert in manager review workflows.
/// </summary>
public enum AuditAlertStatus
{
    /// <summary>Newly surfaced alert pending manager attention.</summary>
    Active = 1,

    /// <summary>Under active manager investigation.</summary>
    Investigating = 2,

    /// <summary>Formally reviewed and acknowledged by a store manager.</summary>
    Reviewed = 3,

    /// <summary>Dismissed with explanation as a false positive or permitted business exception.</summary>
    Dismissed = 4
}
