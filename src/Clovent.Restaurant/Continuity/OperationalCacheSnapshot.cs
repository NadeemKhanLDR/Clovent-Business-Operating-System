using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Root operational cache snapshot containing validated metadata, complete operational payload,
/// and indexed in-memory lookup collections for high-performance sub-millisecond POS access.
/// </summary>
public sealed class OperationalCacheSnapshot
{
    private static readonly byte[] MachineKey = "Clovent-CBOS-OperationalCache-HMAC-Key-v1-Production"u8.ToArray();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>Supported schema version for this client binary.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Cache metadata including provenance and cryptographic signatures.</summary>
    public OperationalCacheMetadata Metadata { get; }

    /// <summary>Cache operational payload.</summary>
    public OperationalCachePayload Payload { get; }

    /// <summary>O(1) dictionary of variants by variant ID.</summary>
    public IReadOnlyDictionary<Guid, CachedVariant> VariantsById { get; }

    /// <summary>O(1) dictionary of categories by category ID.</summary>
    public IReadOnlyDictionary<Guid, CachedCategory> CategoriesById { get; }

    /// <summary>O(1) dictionary of products by product ID.</summary>
    public IReadOnlyDictionary<Guid, CachedProduct> ProductsById { get; }

    /// <summary>Active variants pre-sorted for UI rendering.</summary>
    public IReadOnlyList<CachedVariant> ActiveVariants { get; }

    /// <summary>Active categories pre-sorted for UI rendering.</summary>
    public IReadOnlyList<CachedCategory> ActiveCategories { get; }

    /// <summary>Looks up a variant by ID.</summary>
    public CachedVariant? GetVariant(Guid variantId) =>
        VariantsById.TryGetValue(variantId, out var v) ? v : null;

    /// <summary>Initializes a new instance of <see cref="OperationalCacheSnapshot"/> and indexes all collections.</summary>
    public OperationalCacheSnapshot(OperationalCacheMetadata metadata, OperationalCachePayload payload)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));

        var variantsMap = new Dictionary<Guid, CachedVariant>(payload.Variants.Count);
        foreach (var v in payload.Variants)
        {
            variantsMap[v.ProductVariantId] = v;
        }
        VariantsById = variantsMap;

        var categoriesMap = new Dictionary<Guid, CachedCategory>(payload.Categories.Count);
        foreach (var c in payload.Categories)
        {
            categoriesMap[c.ProductCategoryId] = c;
        }
        CategoriesById = categoriesMap;

        var productsMap = new Dictionary<Guid, CachedProduct>(payload.Products.Count);
        foreach (var p in payload.Products)
        {
            productsMap[p.ProductId] = p;
        }
        ProductsById = productsMap;

        ActiveVariants = payload.Variants
            .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase) && v.IsAvailable)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToList();

        ActiveCategories = payload.Categories
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();
    }

    /// <summary>Searches cached active variants by name, SKU, or product name.</summary>
    public IReadOnlyList<CachedVariant> SearchVariants(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ActiveVariants;
        }

        var term = query.Trim();
        return ActiveVariants
            .Where(v => v.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                     || v.Sku.Contains(term, StringComparison.OrdinalIgnoreCase)
                     || (v.ProductName != null && v.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>Computes a SHA-256 payload checksum from the JSON-serialized payload.</summary>
    public static string ComputePayloadChecksum(OperationalCachePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    /// <summary>Computes the keyed HMAC-SHA256 signature authenticating the cache snapshot.</summary>
    public static string ComputeHmacSignature(
        int schemaVersion,
        string cacheVersion,
        DateTimeOffset generatedAtUtc,
        Guid companyId,
        Guid branchId,
        Guid terminalId,
        Guid warehouseId,
        string payloadChecksum)
    {
        var raw = $"{schemaVersion}:{cacheVersion}:{generatedAtUtc.ToUnixTimeMilliseconds()}:{companyId:N}:{branchId:N}:{terminalId:N}:{warehouseId:N}:{payloadChecksum}";
        var hash = HMACSHA256.HashData(MachineKey, Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    /// <summary>Verifies cryptographic HMAC authentication of this snapshot.</summary>
    public bool VerifyIntegrity()
    {
        var computedChecksum = ComputePayloadChecksum(Payload);
        if (!string.Equals(Metadata.PayloadChecksum, computedChecksum, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var computedHmac = ComputeHmacSignature(
            Metadata.SchemaVersion,
            Metadata.CacheVersion,
            Metadata.GeneratedAtUtc,
            Metadata.CompanyId,
            Metadata.BranchId,
            Metadata.TerminalId,
            Metadata.WarehouseId,
            computedChecksum);

        return string.Equals(Metadata.HmacSignature, computedHmac, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates this snapshot against expected branch, terminal, schema version, and freshness policy.
    /// </summary>
    public (CacheValidationStatus Status, string Message) Validate(
        Guid expectedBranchId,
        Guid expectedTerminalId,
        CacheFreshnessPolicy policy,
        DateTimeOffset? nowUtc = null)
    {
        if (Metadata.SchemaVersion != CurrentSchemaVersion)
        {
            return (CacheValidationStatus.IncompatibleVersion,
                $"Cache schema version {Metadata.SchemaVersion} is incompatible with client schema version {CurrentSchemaVersion}.");
        }

        if (expectedTerminalId != Guid.Empty && Metadata.TerminalId != expectedTerminalId)
        {
            return (CacheValidationStatus.WrongTerminal,
                $"Cache was generated for terminal '{Metadata.TerminalCode}' ({Metadata.TerminalId}), not current terminal {expectedTerminalId}.");
        }

        if (expectedBranchId != Guid.Empty && Metadata.BranchId != expectedBranchId)
        {
            return (CacheValidationStatus.WrongBranch,
                $"Cache was generated for branch '{Metadata.BranchName}' ({Metadata.BranchId}), not current branch {expectedBranchId}.");
        }

        if (!VerifyIntegrity())
        {
            return (CacheValidationStatus.Tampered,
                "Cache cryptographic HMAC authentication or payload checksum failed. Unauthorized tampering detected.");
        }

        var now = nowUtc ?? DateTimeOffset.UtcNow;
        var freshness = policy.EvaluateFreshness(Metadata.GeneratedAtUtc, now);
        var age = now - Metadata.GeneratedAtUtc;

        return freshness switch
        {
            CacheValidationStatus.TooStale => (
                CacheValidationStatus.TooStale,
                $"Operational cache is too stale (age: {age.TotalHours:F1} hours, maximum allowed: {policy.MaxPermittedAge.TotalHours:F0} hours). Selling blocked."),
            CacheValidationStatus.StaleWithinPolicy => (
                CacheValidationStatus.StaleWithinPolicy,
                $"Operational cache is aging (age: {age.TotalHours:F1} hours, warning threshold: {policy.WarningAge.TotalHours:F0} hours). Continuity selling permitted with warning."),
            CacheValidationStatus.Corrupt => (
                CacheValidationStatus.Corrupt,
                "Operational cache timestamp anomaly detected."),
            _ => (CacheValidationStatus.Valid, "Operational cache is valid and fully synchronized.")
        };
    }
}
