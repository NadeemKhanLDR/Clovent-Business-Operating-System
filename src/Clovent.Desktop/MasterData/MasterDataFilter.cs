namespace Clovent.Desktop.MasterData;

/// <summary>
/// Pure search-filter and button-enablement logic for <see cref="MasterDataListView{TDto}"/>,
/// extracted so it can be unit tested without a Windows Forms message loop -
/// the same reasoning already applied to <c>Clovent.Desktop.Navigation.NavigationMenuBuilder</c>
/// being kept separate from <c>MainForm</c>'s DevExpress UI construction.
/// </summary>
public static class MasterDataFilter
{
    /// <summary>Filters <paramref name="items"/> to those whose <paramref name="searchTextSelector"/> result contains <paramref name="searchText"/> (case-insensitive). Returns every item unfiltered if <paramref name="searchText"/> is empty or no selector is supplied.</summary>
    public static IReadOnlyList<TDto> Apply<TDto>(IReadOnlyList<TDto> items, string? searchText, Func<TDto, string>? searchTextSelector)
    {
        var trimmed = searchText?.Trim();
        if (string.IsNullOrEmpty(trimmed) || searchTextSelector is null)
        {
            return items;
        }

        return [.. items.Where(item => searchTextSelector(item).Contains(trimmed, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>Whether the "Edit" button should be enabled.</summary>
    public static bool CanEdit(bool hasFocusedRow, bool? permitted, bool hasHandler) =>
        hasFocusedRow && (permitted ?? true) && hasHandler;

    /// <summary>
    /// Whether the "Activate" button should be enabled. Accepts both
    /// "Inactive" (deliberately deactivated) and "PendingActivation" (a
    /// brand-new <c>User</c>'s starting status, per <c>User.Create</c>) -
    /// without the latter, a newly created user could never be activated
    /// through this button at all, confirmed via manual verification.
    /// </summary>
    public static bool CanActivate(bool hasFocusedRow, bool? permitted, string? status, bool hasHandler) =>
        hasFocusedRow && (permitted ?? true) && (status == "Inactive" || status == "PendingActivation") && hasHandler;

    /// <summary>Whether the "Deactivate" button should be enabled.</summary>
    public static bool CanDeactivate(bool hasFocusedRow, bool? permitted, string? status, bool hasHandler) =>
        hasFocusedRow && (permitted ?? true) && status == "Active" && hasHandler;
}
