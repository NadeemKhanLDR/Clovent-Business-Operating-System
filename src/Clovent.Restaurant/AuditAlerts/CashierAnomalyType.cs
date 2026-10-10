namespace Clovent.Restaurant.AuditAlerts;

/// <summary>
/// Specific behavioral anomaly patterns evaluated by the automated cashier audit engine.
/// </summary>
public enum CashierAnomalyType
{
    /// <summary>
    /// Line voids or full-order cancellations performed after cash payment was recorded or tendered.
    /// Indicates possible cash skimming / sweethearting theft patterns.
    /// </summary>
    ExcessiveVoidsAfterCashTender = 1,

    /// <summary>
    /// An unusually high frequency of manager overrides requested by a specific cashier.
    /// Indicates possible circumvention of pricing controls or internal policy violations.
    /// </summary>
    FrequentManagerOverrides = 2,

    /// <summary>
    /// Cash drawer opening events (e.g. No Sale, manual kick) not linked to completed cash sale transactions.
    /// </summary>
    UnlinkedCashDrawerOpening = 3,

    /// <summary>
    /// High-magnitude or consecutive discount spikes applied by a cashier in a concentrated time window.
    /// </summary>
    UnusualDiscountCluster = 4
}
