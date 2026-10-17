namespace Clovent.MasterData.Support;

/// <summary>
/// Audit trail for authorized support corrective scripts executed against customer databases.
/// Persisted in [MasterData].[SupportScriptExecutionAudit].
/// </summary>
public sealed class SupportScriptExecutionAudit
{
    /// <summary>Surrogate primary key.</summary>
    public long Id { get; private set; }

    /// <summary>Unique identifier of the executed script.</summary>
    public string ScriptId { get; private set; } = string.Empty;

    /// <summary>SHA-256 cryptographic digest of the executed script content.</summary>
    public string ScriptHash { get; private set; } = string.Empty;

    /// <summary>Authenticated database principal or operator who executed the script.</summary>
    public string Operator { get; private set; } = string.Empty;

    /// <summary>UTC timestamp when the script execution completed.</summary>
    public DateTime ExecutedAtUtc { get; private set; }

    /// <summary>Name of the database where the script was executed.</summary>
    public string TargetDatabase { get; private set; } = string.Empty;

    /// <summary>Diagnostic summary of precheck assertions.</summary>
    public string? PreCheckSummary { get; private set; }

    /// <summary>Number of database rows affected by the mutation.</summary>
    public int AffectedRows { get; private set; }

    /// <summary>Execution outcome ('Success' or 'Failed').</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>Error message or failure reason if execution failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Ticket, incident, or dispute compensation reference.</summary>
    public string? CompensationReference { get; private set; }

    /// <summary>Parameterless constructor for EF Core.</summary>
    private SupportScriptExecutionAudit() { }

    /// <summary>Constructs a support script execution audit record.</summary>
    public SupportScriptExecutionAudit(
        string scriptId,
        string scriptHash,
        string @operator,
        DateTime executedAtUtc,
        string targetDatabase,
        string? preCheckSummary,
        int affectedRows,
        string outcome,
        string? errorMessage = null,
        string? compensationReference = null)
    {
        ScriptId = scriptId;
        ScriptHash = scriptHash;
        Operator = @operator;
        ExecutedAtUtc = executedAtUtc;
        TargetDatabase = targetDatabase;
        PreCheckSummary = preCheckSummary;
        AffectedRows = affectedRows;
        Outcome = outcome;
        ErrorMessage = errorMessage;
        CompensationReference = compensationReference;
    }
}
