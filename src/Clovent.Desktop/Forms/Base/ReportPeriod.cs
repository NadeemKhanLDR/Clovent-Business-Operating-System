namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Predefined report period intervals modeled after standard accounting/reporting presets.
/// </summary>
public enum ReportPeriod
{
    /// <summary>The current business calendar day.</summary>
    Today,

    /// <summary>The previous business calendar day.</summary>
    Yesterday,

    /// <summary>The current calendar week.</summary>
    ThisWeek,

    /// <summary>The previous calendar week.</summary>
    LastWeek,

    /// <summary>The current calendar month.</summary>
    ThisMonth,

    /// <summary>The previous calendar month.</summary>
    LastMonth,

    /// <summary>The current calendar quarter.</summary>
    ThisQuarter,

    /// <summary>The previous calendar quarter.</summary>
    LastQuarter,

    /// <summary>The current calendar year.</summary>
    ThisYear,

    /// <summary>The previous calendar year.</summary>
    LastYear,

    /// <summary>The trailing 7 calendar days including today.</summary>
    Last7Days,

    /// <summary>The trailing 30 calendar days including today.</summary>
    Last30Days,

    /// <summary>A user-defined custom date range.</summary>
    Custom
}
