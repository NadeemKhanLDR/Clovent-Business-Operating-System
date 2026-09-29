using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Xunit;

namespace Clovent.Desktop.Tests.Forms.Base;

public class BusinessSettingsAndDisplayTests
{
    private sealed class FakeSession : ICurrentSession
    {
        public bool IsAuthenticated => UserId.HasValue;
        public Guid? UserId { get; set; }
        public Guid? SessionId { get; set; }
        public string? DisplayName { get; set; }
        public event EventHandler? Changed;

        public void SignIn(Guid userId, Guid sessionId, string displayName)
        {
            UserId = userId;
            SessionId = sessionId;
            DisplayName = displayName;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SignOut()
        {
            UserId = null;
            SessionId = null;
            DisplayName = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    [Fact]
    public void DateTimeDisplay_ConvertsUtcToConfiguredBusinessTimeZone()
    {
        // Find or create UTC+05:00 time zone (Pakistan Standard Time)
        TimeZoneInfo pktZone;
        try
        {
            pktZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        }
        catch
        {
            pktZone = TimeZoneInfo.CreateCustomTimeZone("PST-Custom", TimeSpan.FromHours(5), "Pakistan Standard Time", "PKT");
        }

        DateTimeDisplay.Configure(pktZone, "yyyy-MM-dd HH:mm");

        // Shift #1001 opened at 2026-09-14 07:57:16 UTC
        var openedUtc = new DateTimeOffset(2026, 9, 14, 7, 57, 16, TimeSpan.Zero);
        var formatted = DateTimeDisplay.Format(openedUtc);

        // In UTC+5, 07:57 becomes 12:57
        Assert.Equal("2026-09-14 12:57", formatted);
    }

    [Fact]
    public void DateTimeDisplay_DateFormatWithSlashes_PreservesSlashSeparator()
    {
        TimeZoneInfo pktZone;
        try
        {
            pktZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        }
        catch
        {
            pktZone = TimeZoneInfo.CreateCustomTimeZone("PST-Custom", TimeSpan.FromHours(5), "Pakistan Standard Time", "PKT");
        }

        DateTimeDisplay.Configure(pktZone, "dd/MM/yyyy");

        var testDate = new DateOnly(2026, 9, 24);
        var formatted = DateTimeDisplay.FormatDate(testDate);

        Assert.Equal("24/09/2026", formatted);
    }

    [Fact]
    public void CurrencyDisplay_FormatsCorrectlyWithConfiguredSymbolAndCode()
    {
        CurrencyDisplay.Configure("PKR", "Rs.", 2);

        Assert.Equal("Rs.", CurrencyDisplay.SymbolOrCode);
        Assert.Equal("Rs. 53,320.00", CurrencyDisplay.Format(53320m));
        Assert.Equal("Rs. 0.00", CurrencyDisplay.Format(0m));
        Assert.Equal("53,320.00", CurrencyDisplay.FormatPlain(53320m));
    }

    [Fact]
    public void UserDisplayNameHelper_ResolvesCashierCorrectly()
    {
        var session = new FakeSession { DisplayName = "Administrator" };
        Assert.Equal("Administrator", UserDisplayNameHelper.GetCurrentCashierDisplayName(session));

        var emptySession = new FakeSession { DisplayName = "   " };
        Assert.Equal("Cashier", UserDisplayNameHelper.GetCurrentCashierDisplayName(emptySession));

        Assert.Equal("Cashier", UserDisplayNameHelper.GetCurrentCashierDisplayName(null));

        Assert.Equal("admin", UserDisplayNameHelper.FormatCashierName("admin"));
        Assert.Equal("Cashier", UserDisplayNameHelper.FormatCashierName("   "));
        Assert.Equal("Cashier", UserDisplayNameHelper.FormatCashierName(null));
    }

    [Fact]
    public void DesktopDialogSizing_ClampsAndCentersWithinScreen()
    {
        using var form = new Form();
        DesktopDialogSizing.Apply(form, 640, 480, 500, 400, null, false);

        Assert.True(form.ClientSize.Width >= 500);
        Assert.True(form.ClientSize.Height >= 400);

        DesktopDialogSizing.CenterOnOwnerOrScreen(form, null);

        var screen = Screen.FromControl(form) ?? Screen.PrimaryScreen;
        Assert.NotNull(screen);

        Assert.True(form.Left >= screen.WorkingArea.Left);
        Assert.True(form.Top >= screen.WorkingArea.Top);
        Assert.True(form.Right <= screen.WorkingArea.Right);
        Assert.True(form.Bottom <= screen.WorkingArea.Bottom);
    }
}
