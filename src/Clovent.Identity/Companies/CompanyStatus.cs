namespace Clovent.Identity.Companies;

/// <summary>The lifecycle state of a <see cref="Company"/>.</summary>
public enum CompanyStatus
{
    /// <summary>Active and able to operate (own branches, be scoped to).</summary>
    Active,

    /// <summary>Deactivated.</summary>
    Inactive
}
