using System.Resources;

namespace Clovent.Desktop.Forms.Base.Localization;

/// <summary>
/// Localized strings for the Restaurant POS screens - <c>PosStrings.resx</c>
/// (English, the neutral/default resource) and <c>PosStrings.ur.resx</c>
/// (Urdu) are plain <see cref="ResourceManager"/> resources, the standard
/// .NET satellite-assembly mechanism, so adding a third language later is
/// just another <c>PosStrings.{culture}.resx</c> file - no code change here
/// needed. Looked up against <see cref="Thread.CurrentUICulture"/> at call
/// time (not cached), so a language change (see
/// <c>Forms.Restaurant.Setup.RestaurantSetupView</c>) takes effect the next
/// time a screen using these is built - consistent with every POS/Menu
/// Items/Setup screen already being Transient (a fresh instance per
/// navigation, not a singleton kept alive across the change).
/// </summary>
public static class PosStrings
{
    private static readonly ResourceManager Resources = new("Clovent.Desktop.Forms.Base.Localization.PosStrings", typeof(PosStrings).Assembly);

    /// <summary>"NEW DINE-IN" - starts a dine-in order.</summary>
    public static string NewDineIn => Get(nameof(NewDineIn));

    /// <summary>"NEW TAKE AWAY" - starts a take-away order.</summary>
    public static string NewTakeAway => Get(nameof(NewTakeAway));

    /// <summary>"CATEGORIES" - the category rail's header.</summary>
    public static string Categories => Get(nameof(Categories));

    /// <summary>"All Items" - the category rail's "no filter" button.</summary>
    public static string AllItems => Get(nameof(AllItems));

    /// <summary>"Current Bill" - the running order panel's header.</summary>
    public static string CurrentBill => Get(nameof(CurrentBill));

    /// <summary>"Qty" - the order-lines grid's quantity column caption.</summary>
    public static string ColumnQty => Get(nameof(ColumnQty));

    /// <summary>"Item" - the order-lines grid's item-name column caption.</summary>
    public static string ColumnItem => Get(nameof(ColumnItem));

    /// <summary>"Price" - the order-lines grid's unit-price column caption.</summary>
    public static string ColumnPrice => Get(nameof(ColumnPrice));

    /// <summary>"Total" - the order-lines grid's line-total column caption.</summary>
    public static string ColumnTotal => Get(nameof(ColumnTotal));

    /// <summary>"Subtotal" - the totals breakdown's subtotal label.</summary>
    public static string Subtotal => Get(nameof(Subtotal));

    /// <summary>"Discount" - the totals breakdown's discount label.</summary>
    public static string Discount => Get(nameof(Discount));

    /// <summary>"Tax" - the totals breakdown's tax label.</summary>
    public static string Tax => Get(nameof(Tax));

    /// <summary>"Service Charge" - the totals breakdown's service charge label.</summary>
    public static string ServiceCharge => Get(nameof(ServiceCharge));

    /// <summary>"GRAND TOTAL" - the totals breakdown's grand total label.</summary>
    public static string GrandTotal => Get(nameof(GrandTotal));

    /// <summary>"Paid" - the totals breakdown's paid-so-far label.</summary>
    public static string Paid => Get(nameof(Paid));

    /// <summary>"Balance" - the totals breakdown's remaining-balance label.</summary>
    public static string Balance => Get(nameof(Balance));

    /// <summary>"Hold" - the PAY bar's Hold Order button.</summary>
    public static string Hold => Get(nameof(Hold));

    /// <summary>"Clear" - the PAY bar's Clear/Cancel Order button.</summary>
    public static string Clear => Get(nameof(Clear));

    /// <summary>"Print" - the PAY bar's Print Bill button.</summary>
    public static string Print => Get(nameof(Print));

    /// <summary>"More Actions ▾" - the PAY bar's overflow menu button.</summary>
    public static string MoreActions => Get(nameof(MoreActions));

    /// <summary>"PAY" - the dominant Pay button.</summary>
    public static string Pay => Get(nameof(Pay));

    /// <summary>"Send to Kitchen" - the Current Bill panel's Send to Kitchen button.</summary>
    public static string SendToKitchen => Get(nameof(SendToKitchen));

    /// <summary>"Complete Order" - the Current Bill panel's Complete Order button.</summary>
    public static string CompleteOrder => Get(nameof(CompleteOrder));

    /// <summary>"No order selected." - the order-status badge's idle text.</summary>
    public static string NoOrderSelected => Get(nameof(NoOrderSelected));

    /// <summary>The Current Bill panel's empty-cart placeholder text (below the cart emoji).</summary>
    public static string NoItemsAdded => Get(nameof(NoItemsAdded));

    /// <summary>"Location:" - the Location (Warehouse) picker's caption.</summary>
    public static string Location => Get(nameof(Location));

    /// <summary>"Table:" - the Table picker's caption.</summary>
    public static string Table => Get(nameof(Table));

    /// <summary>Looks up <paramref name="key"/> for the current UI culture, falling back to the neutral (English) resource if the key/culture isn't found - never throws for a missing translation.</summary>
    private static string Get(string key) => Resources.GetString(key, Thread.CurrentThread.CurrentUICulture) ?? key;
}
