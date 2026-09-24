using Clovent.Desktop.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class SmartPosStateTests
{
    [Fact]
    public void SuggestionDismissalTracker_Dismiss_HidesVariant()
    {
        var tracker = new SuggestionDismissalTracker();
        var variant = Guid.NewGuid();

        tracker.Dismiss(variant);

        Assert.True(tracker.IsDismissed(variant));
    }

    [Fact]
    public void SuggestionDismissalTracker_UnDismiss_ReadmistsVariant()
    {
        var tracker = new SuggestionDismissalTracker();
        var variant = Guid.NewGuid();
        tracker.Dismiss(variant);

        tracker.UnDismiss(variant);

        Assert.False(tracker.IsDismissed(variant));
    }

    [Fact]
    public void SuggestionDismissalTracker_QuantityOnlyChange_KeepsDismissals()
    {
        var tracker = new SuggestionDismissalTracker();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        tracker.OnBasketChanged([a, b]);
        tracker.Dismiss(a);

        // Same product set (quantities are not part of the signature).
        tracker.OnBasketChanged([a, b]);

        Assert.True(tracker.IsDismissed(a));
    }

    [Fact]
    public void SuggestionDismissalTracker_ProductSetChange_ClearsDismissals()
    {
        var tracker = new SuggestionDismissalTracker();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        tracker.OnBasketChanged([a, b]);
        tracker.Dismiss(a);

        tracker.OnBasketChanged([a, c]);

        Assert.False(tracker.IsDismissed(a));
    }

    [Fact]
    public void SuggestionDismissalTracker_Reset_ClearsEverything()
    {
        var tracker = new SuggestionDismissalTracker();
        var a = Guid.NewGuid();
        tracker.OnBasketChanged([a]);
        tracker.Dismiss(a);

        tracker.Reset();

        Assert.False(tracker.IsDismissed(a));
        // A reset basket must not clear a subsequent dismissal by signature match.
        tracker.OnBasketChanged([a]);
        tracker.Dismiss(a);
        Assert.True(tracker.IsDismissed(a));
    }

    [Fact]
    public void RushModeState_Defaults_OffAndEverythingAllowed()
    {
        var state = new RushModeState();

        Assert.False(state.Enabled);
        Assert.True(state.AllowSuggestionAutoPopup);
        Assert.True(state.AllowTileImages);
        Assert.True(state.AllowSidebarAnimation);
    }

    [Fact]
    public void RushModeState_Enabled_SuppressesPresentationOnly()
    {
        var state = new RushModeState();
        var changed = 0;
        state.Changed += (_, _) => changed++;

        state.Enabled = true;
        state.Enabled = true; // no-op, no duplicate event

        Assert.True(state.Enabled);
        Assert.False(state.AllowSuggestionAutoPopup);
        Assert.False(state.AllowTileImages);
        Assert.False(state.AllowSidebarAnimation);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void SearchResultsCache_ReturnsCachedEntryWithinLifetime()
    {
        var cache = new SearchResultsCache<string>(TimeSpan.FromSeconds(10));

        cache.Set("chai", "hit");
        var found = cache.TryGet("chai", out var results);

        Assert.True(found);
        Assert.Equal("hit", results);
    }

    [Fact]
    public void SearchResultsCache_MissesUnknownAndExpiredTerms()
    {
        var cache = new SearchResultsCache<string>(TimeSpan.FromMilliseconds(1));

        cache.Set("chai", "hit");
        System.Threading.Thread.Sleep(10);

        Assert.False(cache.TryGet("chai", out _));
        Assert.False(cache.TryGet("unknown", out _));
        Assert.False(cache.TryGet("", out _));
        Assert.False(cache.TryGet("   ", out _));
    }

    [Fact]
    public void SearchResultsCache_IgnoresTermCaseAndWhitespace()
    {
        var cache = new SearchResultsCache<string>(TimeSpan.FromSeconds(10));

        cache.Set("chai", "hit");

        Assert.True(cache.TryGet("  CHAI ", out _));
    }
}
