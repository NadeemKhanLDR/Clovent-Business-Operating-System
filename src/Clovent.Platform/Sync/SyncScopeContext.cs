namespace Clovent.Platform.Sync;

/// <summary>
/// Operational scope context defining local terminal, branch, and organization tenancy identities.
/// Used at ingestion to validate inbound sync packets and prevent cross-tenant replication or origin self-loops.
/// </summary>
public sealed class SyncScopeContext
{
    /// <summary>The organization tenancy hosting this node.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>The branch location hosting this node.</summary>
    public Guid BranchId { get; set; }

    /// <summary>The local physical terminal identity.</summary>
    public Guid TerminalId { get; set; }

    /// <summary>Maximum supported envelope schema version.</summary>
    public int SupportedSchemaVersion { get; set; } = 1;

    /// <summary>Creates a default scope context.</summary>
    public SyncScopeContext(Guid organizationId = default, Guid branchId = default, Guid terminalId = default, int supportedSchemaVersion = 1)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        TerminalId = terminalId;
        SupportedSchemaVersion = supportedSchemaVersion;
    }
}
