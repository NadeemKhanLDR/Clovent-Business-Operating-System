using System;
using Clovent.Desktop.Forms.Base;
using Xunit;

namespace Clovent.Desktop.Tests.Forms.Base;

public class ReportPeriodCalculatorTests
{
    private static readonly DateOnly StandardDate = new(2026, 9, 22); // Tuesday, Sep 22, 2026 (Q3)

    [Fact]
    public void Today_ReturnsSameDate()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.Today, StandardDate);
        Assert.Equal(new DateOnly(2026, 9, 22), range.From);
        Assert.Equal(new DateOnly(2026, 9, 22), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void Yesterday_ReturnsPreviousDay()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.Yesterday, StandardDate);
        Assert.Equal(new DateOnly(2026, 9, 21), range.From);
        Assert.Equal(new DateOnly(2026, 9, 21), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void ThisWeek_StartingMonday_ReturnsMondayToSunday()
    {
        // 2026-09-22 is Tuesday -> Monday is 2026-09-21, Sunday is 2026-09-27
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisWeek, StandardDate);
        Assert.Equal(new DateOnly(2026, 9, 21), range.From);
        Assert.Equal(new DateOnly(2026, 9, 27), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void LastWeek_StartingMonday_ReturnsPriorMondayToSunday()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastWeek, StandardDate);
        Assert.Equal(new DateOnly(2026, 9, 14), range.From);
        Assert.Equal(new DateOnly(2026, 9, 20), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void ThisMonth_ReturnsFirstToLastDayOfMonth()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisMonth, StandardDate);
        Assert.Equal(new DateOnly(2026, 9, 1), range.From);
        Assert.Equal(new DateOnly(2026, 9, 30), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void LastMonth_ReturnsFirstToLastDayOfPriorMonth()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastMonth, StandardDate);
        Assert.Equal(new DateOnly(2026, 8, 1), range.From);
        Assert.Equal(new DateOnly(2026, 8, 31), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void ThisQuarter_ForSeptember_ReturnsQ3Bounds()
    {
        // September is in Q3: July 1 to September 30
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisQuarter, StandardDate);
        Assert.Equal(new DateOnly(2026, 7, 1), range.From);
        Assert.Equal(new DateOnly(2026, 9, 30), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void LastQuarter_ForSeptember_ReturnsQ2Bounds()
    {
        // Prior to Q3 is Q2: April 1 to June 30
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastQuarter, StandardDate);
        Assert.Equal(new DateOnly(2026, 4, 1), range.From);
        Assert.Equal(new DateOnly(2026, 6, 30), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void ThisYear_ReturnsJanuary1ToDecember31()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisYear, StandardDate);
        Assert.Equal(new DateOnly(2026, 1, 1), range.From);
        Assert.Equal(new DateOnly(2026, 12, 31), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void LastYear_ReturnsPriorJanuary1ToDecember31()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastYear, StandardDate);
        Assert.Equal(new DateOnly(2025, 1, 1), range.From);
        Assert.Equal(new DateOnly(2025, 12, 31), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void Last7Days_ReturnsSevenDaysSpanEndingToday()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.Last7Days, StandardDate);
        Assert.Equal(new DateOnly(2026, 9, 16), range.From);
        Assert.Equal(new DateOnly(2026, 9, 22), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void Last30Days_ReturnsThirtyDaysSpanEndingToday()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.Last30Days, StandardDate);
        Assert.Equal(new DateOnly(2026, 8, 24), range.From);
        Assert.Equal(new DateOnly(2026, 9, 22), range.To);
        Assert.True(range.IsValid);
    }

    [Fact]
    public void Custom_ReturnsReferenceDateByDefault()
    {
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.Custom, StandardDate);
        Assert.Equal(StandardDate, range.From);
        Assert.Equal(StandardDate, range.To);
    }

    [Fact]
    public void MonthBoundary_FirstDayOfMonth_CalculatesCorrectPriorMonth()
    {
        var date = new DateOnly(2026, 1, 1);
        var lastMonth = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastMonth, date);
        Assert.Equal(new DateOnly(2025, 12, 1), lastMonth.From);
        Assert.Equal(new DateOnly(2025, 12, 31), lastMonth.To);

        var thisMonth = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisMonth, date);
        Assert.Equal(new DateOnly(2026, 1, 1), thisMonth.From);
        Assert.Equal(new DateOnly(2026, 1, 31), thisMonth.To);
    }

    [Fact]
    public void YearBoundary_NewYearsDay_CalculatesCorrectPriorYearAndQuarter()
    {
        var date = new DateOnly(2026, 1, 1);
        var thisQuarter = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisQuarter, date);
        Assert.Equal(new DateOnly(2026, 1, 1), thisQuarter.From);
        Assert.Equal(new DateOnly(2026, 3, 31), thisQuarter.To);

        var lastQuarter = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastQuarter, date);
        Assert.Equal(new DateOnly(2025, 10, 1), lastQuarter.From);
        Assert.Equal(new DateOnly(2025, 12, 31), lastQuarter.To);

        var lastYear = ReportPeriodCalculator.CalculateRange(ReportPeriod.LastYear, date);
        Assert.Equal(new DateOnly(2025, 1, 1), lastYear.From);
        Assert.Equal(new DateOnly(2025, 12, 31), lastYear.To);
    }

    [Fact]
    public void QuarterBoundary_LastDayOfQuarter_CalculatesAccurately()
    {
        var q1End = new DateOnly(2026, 3, 31);
        var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisQuarter, q1End);
        Assert.Equal(new DateOnly(2026, 1, 1), range.From);
        Assert.Equal(new DateOnly(2026, 3, 31), range.To);

        var q2End = new DateOnly(2026, 6, 30);
        var q2Range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisQuarter, q2End);
        Assert.Equal(new DateOnly(2026, 4, 1), q2Range.From);
        Assert.Equal(new DateOnly(2026, 6, 30), q2Range.To);
    }

    [Fact]
    public void LeapYear_February29_CalculatedCorrectly()
    {
        var leapFeb = new DateOnly(2024, 2, 15);
        var leapRange = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisMonth, leapFeb);
        Assert.Equal(new DateOnly(2024, 2, 1), leapRange.From);
        Assert.Equal(new DateOnly(2024, 2, 29), leapRange.To);

        var nonLeapFeb = new DateOnly(2023, 2, 15);
        var nonLeapRange = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisMonth, nonLeapFeb);
        Assert.Equal(new DateOnly(2023, 2, 1), nonLeapRange.From);
        Assert.Equal(new DateOnly(2023, 2, 28), nonLeapRange.To);
    }

    [Fact]
    public void InvalidCustomRange_FromGreaterThanTo_IsDetectedAsInvalid()
    {
        var invalidRange = new DateRange(new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 20));
        Assert.False(invalidRange.IsValid);

        var validRange = new DateRange(new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 25));
        Assert.True(validRange.IsValid);
    }

    [Fact]
    public void DateRange_UtcBounds_PreserveFullDaySemantics()
    {
        var range = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 22));

        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), range.StartOfFromUtc);
        Assert.Equal(new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero), range.StartOfDayAfterToUtc);
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 23, 59, 59, TimeSpan.Zero).AddTicks(9999999), range.EndOfToUtcInclusive);
    }
}
