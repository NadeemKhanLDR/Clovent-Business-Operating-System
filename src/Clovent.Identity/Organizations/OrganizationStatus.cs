namespace Clovent.Identity.Organizations;

/// <summary>The lifecycle state of an <see cref="Organization"/>.</summary>
public enum OrganizationStatus
{
    /// <summary>Active and able to operate (own companies, be scoped to).</summary>
    Active,

    /// <summary>Deactivated - e.g. a tenant offboarded or suspended.</summary>
    Inactive
}
