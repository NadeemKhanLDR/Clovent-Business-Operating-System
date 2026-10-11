namespace Clovent.Desktop.Navigation;

/// <summary>
/// Top-level business-oriented Ribbon page captions for CBOS shell navigation.
/// Order and naming conform to the CBOS Back Office Information Architecture standard.
/// </summary>
public static class NavigationPage
{
    /// <summary>Caption for the Masters ribbon page.</summary>
    public const string Masters = "Masters";

    /// <summary>Caption for the Inventory ribbon page.</summary>
    public const string Inventory = "Inventory";

    /// <summary>Caption for the Purchases ribbon page.</summary>
    public const string Purchases = "Purchases";

    /// <summary>Caption for the POS ribbon page.</summary>
    public const string Pos = "POS";

    /// <summary>Caption for the Manager Panel ribbon page.</summary>
    public const string ManagerPanel = "Manager Panel";

    /// <summary>Caption for the Users ribbon page.</summary>
    public const string Users = "Users";

    /// <summary>Caption for the Reports ribbon page.</summary>
    public const string Reports = "Reports";

    /// <summary>Caption for the Settings ribbon page.</summary>
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
