namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Immutable metadata identifying a local operational cache snapshot.
/// Contains provenance, terminal scope, versioning, item counts, and cryptographic authentication.
/// </summary>
public sealed record OperationalCacheMetadata(
    int SchemaVersion,
    string CacheVersion,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset LastSuccessfulSyncUtc,
    Guid CompanyId,
    string CompanyName,
    Guid BranchId,
    string BranchName,
    Guid TerminalId,
    string TerminalName,
    string TerminalCode,
    Guid WarehouseId,
    string WarehouseName,
    string CurrencyCode,
    string CurrencySymbol,
    int CurrencyDecimalPlaces,
    string SourceDatabaseIdentity,
    string SourceRevision,
    int TotalCategories,
    int TotalProducts,
    int TotalVariants,
    int TotalTemplates,
    string PayloadChecksum,
    string HmacSignature);
