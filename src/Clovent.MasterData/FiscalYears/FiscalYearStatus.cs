namespace Clovent.MasterData.FiscalYears;

/// <summary>
/// The lifecycle state of a <see cref="FiscalYear"/> - deliberately its own
/// two-value enum rather than reusing <see cref="Shared.MasterDataStatus"/>:
/// "Open"/"Closed" (a one-way books-closing transition) is a different
/// concept from "Active"/"Inactive" (a reversible availability toggle), even
/// though both happen to have two cases.
/// </summary>
public enum FiscalYearStatus
{
    /// <summary>Open - transactions may still post against this fiscal year.</summary>
    Open,

    /// <summary>Closed - the books are final; this transition does not reverse.</summary>
    Closed
}
