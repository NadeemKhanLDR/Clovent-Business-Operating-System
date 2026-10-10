namespace Clovent.Platform.Sync;

/// <summary>Standard entity kinds replicated across multi-terminal retail setups.</summary>
public static class SyncEntityKinds
{
    /// <summary>Relative or absolute warehouse stock balance movement.</summary>
    public const string InventoryStockDelta = "InventoryStockDelta";

    /// <summary>Catalog price adjustment across selling or cost prices.</summary>
    public const string CatalogPriceAdjustment = "CatalogPriceAdjustment";

    /// <summary>Cash register shift opening/closing summary and drawer reconciliation.</summary>
    public const string ShiftSummary = "ShiftSummary";

    /// <summary>Completed sales order transaction delta.</summary>
    public const string SalesTransactionDelta = "SalesTransactionDelta";

    /// <summary>Master data or configuration delta.</summary>
    public const string MasterDataDelta = "MasterDataDelta";
}

/// <summary>Replication operation semantics for a sync delta.</summary>
public static class SyncOperations
{
    /// <summary>Commutative relative delta (e.g. +5 or -2 quantity) guaranteeing zero drift under concurrent ingestion.</summary>
    public const string Delta = "Delta";

    /// <summary>Absolute state snapshot / replacement subject to strict concurrency token checks.</summary>
    public const string Snapshot = "Snapshot";

    /// <summary>Manager-approved stock adjustment or price override.</summary>
    public const string Adjustment = "Adjustment";

    /// <summary>Deactivation or soft-deletion of an entity.</summary>
    public const string Deactivation = "Deactivation";
}
