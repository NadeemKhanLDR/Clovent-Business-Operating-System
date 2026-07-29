namespace Clovent.MasterData.Shared;

/// <summary>
/// Shared lifecycle state for the aggregates in this bounded context that
/// only ever need "active or not" (<see cref="Departments.Department"/>,
/// <see cref="Warehouses.Warehouse"/>, <see cref="Terminals.Terminal"/>,
/// <see cref="Currencies.Currency"/>, <see cref="Languages.Language"/>,
/// <see cref="TimeZones.TimeZoneEntry"/>) - one enum rather than six
/// structurally-identical ones, since (unlike <c>Clovent.Identity</c>'s
/// per-aggregate name value objects, kept distinct deliberately for
/// compile-time type safety) an enum carries no such risk: <c>MasterDataStatus.Active</c>
/// means the same thing regardless of which aggregate reads it.
/// <see cref="Identity.Organizations.OrganizationStatus"/>/<c>CompanyStatus</c>/<c>BranchStatus</c>
/// stay separate, per-aggregate enums in their own bounded context - not
/// reused here, to avoid an unnecessary cross-project dependency for what
/// would otherwise be identical two-value enums.
/// </summary>
public enum MasterDataStatus
{
    /// <summary>Active and usable.</summary>
    Active,

    /// <summary>Deactivated.</summary>
    Inactive
}
