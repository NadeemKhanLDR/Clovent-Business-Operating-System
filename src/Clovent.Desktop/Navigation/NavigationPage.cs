namespace Clovent.Desktop.Navigation;

/// <summary>
/// Top-level business-oriented Ribbon page captions for CBOS shell navigation.
/// Order and naming conform to the CBOS Back Office Information Architecture standard.
/// </summary>
public static class NavigationPage
{
    public const string Masters = "Masters";
    public const string Inventory = "Inventory";
    public const string Purchases = "Purchases";
    public const string Pos = "POS";
    public const string ManagerPanel = "Manager Panel";
    public const string Users = "Users";
    public const string Reports = "Reports";
    public const string Settings = "Settings";

    /// <summary>
    /// The canonical list of all top-level ribbon pages in strictly ordered sequence.
    /// </summary>
    public static readonly IReadOnlyList<string> OrderedPages =
    [
        Masters,
        Inventory,
        Purchases,
        Pos,
        ManagerPanel,
        Users,
        Reports,
        Settings
    ];

    /// <summary>
    /// Pages whose visibility is permission-gated based on whether the signed-in user
    /// has at least one visible item in any group. Masters is always open (carries session/recent/notifications/workspace).
    /// </summary>
    public static readonly IReadOnlyList<string> PermissionGatedPages =
    [
        Inventory,
        Purchases,
        Pos,
        ManagerPanel,
        Users,
        Reports,
        Settings
    ];
}
