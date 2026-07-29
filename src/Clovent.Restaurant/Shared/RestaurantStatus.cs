namespace Clovent.Restaurant.Shared;

/// <summary>
/// The lifecycle state shared by every Restaurant POS reference-data
/// aggregate (<see cref="DiningAreas.DiningArea"/>, <see cref="Tables.Table"/>,
/// <c>PaymentMethods.PaymentMethod</c>) - deliberately not reusing
/// <c>Clovent.MasterData.Shared.MasterDataStatus</c> even though the shape is
/// identical, the same "avoid an unnecessary cross-project dependency"
/// reasoning <c>Clovent.Catalog.Shared.CatalogStatus</c>'s doc comment
/// already applies (this project's own dependency on <c>Clovent.MasterData</c>
/// exists only for <c>WarehouseId</c>/<c>EntityCode</c> reuse, not to share
/// its status vocabulary).
/// </summary>
public enum RestaurantStatus
{
    /// <summary>The record is in normal use.</summary>
    Active,

    /// <summary>The record is retired - hidden from normal selection, its history preserved.</summary>
    Inactive
}
