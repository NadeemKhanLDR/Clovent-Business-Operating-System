namespace Clovent.Restaurant.AuditAlerts;

/// <summary>
/// Operational severity ranking for cashier audit alerts.
/// </summary>
public enum AuditAlertSeverity
{
    /// <summary>Informational anomaly requiring passive logging.</summary>
    Low = 1,

    /// <summary>Moderate behavioral deviation warranting shift review.</summary>
    Medium = 2,

    /// <summary>High-probability shrinkage risk requiring manager investigation.</summary>
    High = 3,

    /// <summary>Critical financial irregularity requiring immediate managerial intervention.</summary>
    Critical = 4
}
