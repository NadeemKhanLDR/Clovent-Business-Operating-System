using System;
using System.Collections.Generic;
using System.Linq;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Tracks which smart-suggestion variants the cashier dismissed for the
/// current basket. Dismissals are per basket: they reset when a different
/// order becomes current, or when the basket's product composition changes
/// beyond mere quantity tweaks (a new or removed product).
/// Pure state - no UI, no timers - so unit tests can drive it directly.
/// </summary>
internal sealed class SuggestionDismissalTracker
{
    private readonly HashSet<Guid> _dismissedVariantIds = [];
    private string? _basketSignature;

    /// <summary>Registers the current basket; clears dismissals when the product set changed.</summary>
    public void OnBasketChanged(IEnumerable<Guid> basketVariantIds)
    {
        var signature = string.Join("|", basketVariantIds.Distinct().OrderBy(id => id));
        if (string.Equals(signature, _basketSignature, StringComparison.Ordinal))
        {
            return;
        }

        _basketSignature = signature;
        _dismissedVariantIds.Clear();
    }

    /// <summary>Clears all dismissals (new order recalled/created/cleared).</summary>
    public void Reset()
    {
        _dismissedVariantIds.Clear();
        _basketSignature = null;
    }

    /// <summary>Marks a suggestion as dismissed for the current basket.</summary>
    public void Dismiss(Guid variantId) => _dismissedVariantIds.Add(variantId);

    /// <summary>Re-admits a variant after the cashier explicitly added it from the strip.</summary>
    public void UnDismiss(Guid variantId) => _dismissedVariantIds.Remove(variantId);

    /// <summary>Whether the variant was dismissed for the current basket.</summary>
    public bool IsDismissed(Guid variantId) => _dismissedVariantIds.Contains(variantId);
}

/// <summary>
/// Presentation-only Rush Mode state: when enabled the POS skips decorative
/// work (sidebar collapse animation, tile image loads, suggestion auto-popup)
/// without touching any business logic. Testable without a form.
/// </summary>
internal sealed class RushModeState
{
    private bool _enabled;

    /// <summary>Raised after <see cref="Enabled"/> changes so the host can apply visuals.</summary>
    public event EventHandler? Changed;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Suggestion strip must not pop up on its own while rush mode is on.</summary>
    public bool AllowSuggestionAutoPopup => !Enabled;

    /// <summary>Skip loading tile images while rush mode is on.</summary>
    public bool AllowTileImages => !Enabled;

    /// <summary>Skip animated sidebar collapse/expand while rush mode is on.</summary>
    public bool AllowSidebarAnimation => !Enabled;
}

/// <summary>
/// Small per-term cache for universal search results so backspacing through a
/// recently searched term does not re-query. Entries expire after a fixed
/// sliding lifetime.
/// </summary>
internal sealed class SearchResultsCache<T>
{
    private readonly TimeSpan _lifetime;
    private readonly Dictionary<string, (DateTimeOffset ExpiresAt, T Results)> _entries = new(StringComparer.OrdinalIgnoreCase);

    public SearchResultsCache(TimeSpan lifetime)
    {
        _lifetime = lifetime;
    }

    public bool TryGet(string term, out T? results)
    {
        results = default;
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        if (_entries.TryGetValue(term.Trim(), out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                results = entry.Results;
                return true;
            }

            _entries.Remove(term.Trim());
        }

        return false;
    }

    public void Set(string term, T results)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        // Keep the cache small - it exists for backspace patterns only.
        if (_entries.Count > 32)
        {
            _entries.Clear();
        }

        _entries[term.Trim()] = (DateTimeOffset.UtcNow + _lifetime, results);
    }
}
