namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

/// <summary>
/// Broadcasts "menu items changed" across the Desktop process so every open
/// document that shows menu items (chiefly <c>RestaurantPosView</c>'s
/// product tile wall) can refresh itself without the cashier having to close
/// and reopen POS. Registered as a Singleton, unlike
/// <c>MenuItemsForm</c>/<c>RestaurantPosView</c> themselves (both Transient,
/// a fresh instance per navigation per
/// <c>docs/architecture/RestaurantPOSArchitecture.md</c> Section 12) - the
/// notification channel has to outlive any one subscriber as documents open
/// and close.
/// </summary>
public interface IMenuItemsChangeNotifier
{
    /// <summary>Raised after a menu item or category is created, edited, activated, or deactivated.</summary>
    event EventHandler? Changed;

    /// <summary>Raises <see cref="Changed"/>. Called by <c>MenuItemsForm</c> after each mutating action succeeds - never from a refresh/load path, to avoid a notify-refresh-notify loop.</summary>
    void NotifyChanged();
}

/// <inheritdoc cref="IMenuItemsChangeNotifier"/>
public sealed class MenuItemsChangeNotifier : IMenuItemsChangeNotifier
{
    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
