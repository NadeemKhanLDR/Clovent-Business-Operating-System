using System;
using System.Collections.Generic;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Deterministic date range calculator for predefined accounting report periods.
/// Accepts an explicit business reference date so it is fully testable across leap years,
/// quarter boundaries, and year transitions without depending on system clock.
/// </summary>
public static class ReportPeriodCalculator
{
    private static readonly (ReportPeriod Period, string DisplayName)[] Options =
    [
        (ReportPeriod.Today, "Today"),
        (ReportPeriod.Yesterday, "Yesterday"),
        (ReportPeriod.ThisWeek, "This Week"),
        (ReportPeriod.LastWeek, "Last Week"),
        (ReportPeriod.ThisMonth, "This Month"),
        (ReportPeriod.LastMonth, "Last Month"),
        (ReportPeriod.ThisQuarter, "This Quarter"),
        (ReportPeriod.LastQuarter, "Last Quarter"),
        (ReportPeriod.ThisYear, "This Year"),
        (ReportPeriod.LastYear, "Last Year"),
        (ReportPeriod.Last7Days, "Last 7 Days"),
        (ReportPeriod.Last30Days, "Last 30 Days"),
        (ReportPeriod.Custom, "Custom")
    ];

    public static IReadOnlyList<(ReportPeriod Period, string DisplayName)> GetAllOptions() => Options;

    public static string GetDisplayName(ReportPeriod period) => period switch
    {
        ReportPeriod.Today => "Today",
        ReportPeriod.Yesterday => "Yesterday",
        ReportPeriod.ThisWeek => "This Week",
        ReportPeriod.LastWeek => "Last Week",
        ReportPeriod.ThisMonth => "This Month",
        ReportPeriod.LastMonth => "Last Month",
        ReportPeriod.ThisQuarter => "This Quarter",
        ReportPeriod.LastQuarter => "Last Quarter",
        ReportPeriod.ThisYear => "This Year",
        ReportPeriod.LastYear => "Last Year",
        ReportPeriod.Last7Days => "Last 7 Days",
        ReportPeriod.Last30Days => "Last 30 Days",
        ReportPeriod.Custom => "Custom",
        _ => period.ToString()
    };

    public static ReportPeriod ParseDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return ReportPeriod.Custom;
        }

        foreach (var (p, name) in Options)
        {
            if (string.Equals(displayName.Trim(), name, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return ReportPeriod.Custom;
    }

    /// <summary>
    /// Calculates the <see cref="DateRange"/> for the specified <paramref name="period"/>
    /// relative to <paramref name="referenceDate"/>.
    /// </summary>
    public static DateRange CalculateRange(ReportPeriod period, DateOnly referenceDate, DayOfWeek weekStartsOn = DayOfWeek.Monday)
    {
        switch (period)
        {
            case ReportPeriod.Today:
                return new DateRange(referenceDate, referenceDate);

            case ReportPeriod.Yesterday:
                var yesterday = referenceDate.AddDays(-1);
                return new DateRange(yesterday, yesterday);

            case ReportPeriod.ThisWeek:
            {
                int diff = (7 + (referenceDate.DayOfWeek - weekStartsOn)) % 7;
                var startOfWeek = referenceDate.AddDays(-diff);
                var endOfWeek = startOfWeek.AddDays(6);
                return new DateRange(startOfWeek, endOfWeek);
            }

            case ReportPeriod.LastWeek:
            {
                int diff = (7 + (referenceDate.DayOfWeek - weekStartsOn)) % 7;
                var startOfThisWeek = referenceDate.AddDays(-diff);
                var startOfLastWeek = startOfThisWeek.AddDays(-7);
                var endOfLastWeek = startOfLastWeek.AddDays(6);
                return new DateRange(startOfLastWeek, endOfLastWeek);
            }

            case ReportPeriod.ThisMonth:
            {
                var startOfMonth = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
                var endOfMonth = new DateOnly(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));
                return new DateRange(startOfMonth, endOfMonth);
            }

            case ReportPeriod.LastMonth:
            {
                int prevYear = referenceDate.Month == 1 ? referenceDate.Year - 1 : referenceDate.Year;
                int prevMonth = referenceDate.Month == 1 ? 12 : referenceDate.Month - 1;
                var startOfLastMonth = new DateOnly(prevYear, prevMonth, 1);
                var endOfLastMonth = new DateOnly(prevYear, prevMonth, DateTime.DaysInMonth(prevYear, prevMonth));
                return new DateRange(startOfLastMonth, endOfLastMonth);
            }

            case ReportPeriod.ThisQuarter:
            {
                int quarter = (referenceDate.Month - 1) / 3 + 1;
                int startMonth = (quarter - 1) * 3 + 1;
                int endMonth = startMonth + 2;
                var startOfQuarter = new DateOnly(referenceDate.Year, startMonth, 1);
                var endOfQuarter = new DateOnly(referenceDate.Year, endMonth, DateTime.DaysInMonth(referenceDate.Year, endMonth));
                return new DateRange(startOfQuarter, endOfQuarter);
            }

            case ReportPeriod.LastQuarter:
            {
                int quarter = (referenceDate.Month - 1) / 3 + 1;
                int prevQYear = quarter == 1 ? referenceDate.Year - 1 : referenceDate.Year;
                int prevQuarter = quarter == 1 ? 4 : quarter - 1;
                int startMonth = (prevQuarter - 1) * 3 + 1;
                int endMonth = startMonth + 2;
                var startOfLastQ = new DateOnly(prevQYear, startMonth, 1);
                var endOfLastQ = new DateOnly(prevQYear, endMonth, DateTime.DaysInMonth(prevQYear, endMonth));
                return new DateRange(startOfLastQ, endOfLastQ);
            }

            case ReportPeriod.ThisYear:
                return new DateRange(new DateOnly(referenceDate.Year, 1, 1), new DateOnly(referenceDate.Year, 12, 31));

            case ReportPeriod.LastYear:
                return new DateRange(new DateOnly(referenceDate.Year - 1, 1, 1), new DateOnly(referenceDate.Year - 1, 12, 31));

            case ReportPeriod.Last7Days:
                return new DateRange(referenceDate.AddDays(-6), referenceDate);

            case ReportPeriod.Last30Days:
                return new DateRange(referenceDate.AddDays(-29), referenceDate);

            case ReportPeriod.Custom:
            default:
                return new DateRange(referenceDate, referenceDate);
        }
    }
}
