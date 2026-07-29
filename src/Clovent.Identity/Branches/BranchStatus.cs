namespace Clovent.Identity.Branches;

/// <summary>The lifecycle state of a <see cref="Branch"/>.</summary>
public enum BranchStatus
{
    /// <summary>Active and able to operate (own departments/warehouses/terminals, be scoped to).</summary>
    Active,

    /// <summary>Deactivated.</summary>
    Inactive
}
