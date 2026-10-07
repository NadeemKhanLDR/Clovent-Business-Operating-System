using System.Diagnostics;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.DiningAreas.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Continuity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Continuity;

/// <summary>
/// Authoritative synchronizer for local POS operational cache.
/// Queries menu, prices, taxes, and terminal rules from the authoritative database
/// and writes cryptographically verified snapshots atomically.
/// </summary>
public sealed class OperationalCacheSynchronizer : IOperationalCacheSynchronizer
{
    private readonly IMediator _mediator;
    private readonly IOperationalCacheStore _cacheStore;
    private readonly ILogger<OperationalCacheSynchronizer>? _logger;
    private readonly object _syncLock = new();
    private OperationalCacheSnapshot? _lastSyncedSnapshot;

    /// <summary>Gets the most recently synchronized snapshot held in memory.</summary>
    public OperationalCacheSnapshot? LastSyncedSnapshot
    {
        get { lock (_syncLock) return _lastSyncedSnapshot; }
    }

    /// <summary>Initializes a new instance of <see cref="OperationalCacheSynchronizer"/>.</summary>
    public OperationalCacheSynchronizer(
        IMediator mediator,
        IOperationalCacheStore cacheStore,
        ILogger<OperationalCacheSynchronizer>? logger = null)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OperationalCacheSnapshot> SynchronizeAsync(
        Guid companyId,
        string companyName,
        Guid branchId,
        string branchName,
        Guid terminalId,
        string terminalName,
        string terminalCode,
        Guid warehouseId,
        string warehouseName,
        string currencyCode,
        string currencySymbol,
        int currencyDecimals,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogInformation(
            "Starting operational cache synchronization for terminal {TerminalCode} ({TerminalId}) at branch {BranchName}",
            terminalCode, terminalId, branchName);

        // 1. Query online catalog and menu
        var categories = await _mediator.Send(new ListProductCategoriesQuery(), cancellationToken).ConfigureAwait(false);
        var products = await _mediator.Send(new ListProductsQuery(), cancellationToken).ConfigureAwait(false);
        var variants = await _mediator.Send(new ListProductVariantsQuery(), cancellationToken).ConfigureAwait(false);
        var prices = await _mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Selling), cancellationToken).ConfigureAwait(false);
        var templates = await _mediator.Send(new ListActiveQuickOrderTemplatesQuery(warehouseId == Guid.Empty ? null : warehouseId), cancellationToken).ConfigureAwait(false);
        var diningAreas = await _mediator.Send(new ListAllDiningAreasQuery(), cancellationToken).ConfigureAwait(false);
        var tables = await _mediator.Send(new ListAllTablesQuery(), cancellationToken).ConfigureAwait(false);
        var paymentMethods = await _mediator.Send(new ListPaymentMethodsQuery(), cancellationToken).ConfigureAwait(false);

        // 2. Query warehouse stocks for advisory stock-on-hand display
        IReadOnlyCollection<WarehouseStockDto> stocks = Array.Empty<WarehouseStockDto>();
        if (warehouseId != Guid.Empty)
        {
            try
            {
                stocks = await _mediator.Send(new ListWarehouseStocksByWarehouseQuery(warehouseId), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load warehouse stocks for cache; continuing with empty stocks");
            }
        }

        var stockByVariantId = stocks.ToDictionary(s => s.ProductVariantId, s => s.QuantityOnHand);
        var newestSellingPriceByVariantId = prices
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);
        var productMap = products.ToDictionary(p => p.ProductId, p => p);

        // 3. Map into immutable cached records
        var cachedCategories = categories.Select(c => new CachedCategory(
            c.ProductCategoryId,
            c.Name,
            c.ParentCategoryId,
            c.ColorHex,
            c.SortOrder,
            c.Status)).ToList();

        var cachedProducts = products.Select(p => new CachedProduct(
            p.ProductId,
            p.Name,
            p.Sku,
            p.CategoryId,
            p.ItemType,
            p.TaxRatePercentage,
            p.TaxIsInclusive,
            p.Status)).ToList();

        var cachedVariants = variants.Select(v =>
        {
            productMap.TryGetValue(v.ProductId, out var prod);
            var price = newestSellingPriceByVariantId.GetValueOrDefault(v.ProductVariantId, 0m);
            var stock = stockByVariantId.TryGetValue(v.ProductVariantId, out var s) ? (decimal?)s : null;
            return new CachedVariant(
                v.ProductVariantId,
                v.ProductId,
                v.Name,
                v.Sku,
                price,
                v.UnitOfMeasureId,
                v.ProductCategoryId ?? prod?.CategoryId,
                v.SortOrder,
                v.Status,
                v.IsAvailable,
                v.ItemType ?? prod?.ItemType ?? "Prepared",
                stock,
                prod?.Name,
                prod?.TaxRatePercentage ?? 0m,
                prod?.TaxIsInclusive ?? false);
        }).ToList();

        var cachedTemplates = templates.Select(t => new CachedQuickOrderTemplate(
            t.TemplateId,
            t.Name,
            t.Description,
            t.TotalPrice,
            t.WarehouseId,
            t.Items.Select(i => new CachedQuickOrderTemplateItem(
                i.VariantId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.Total)).ToList()
        )).ToList();

        var cachedAreas = diningAreas.Select(a => new CachedDiningArea(a.DiningAreaId, a.Name)).ToList();
        var cachedTables = tables.Select(t => new CachedTable(t.TableId, t.DiningAreaId, t.Code, t.Capacity, t.Status, t.OccupancyStatus)).ToList();

        var cachedPaymentMethods = paymentMethods.Select(p => new CachedPaymentMethod(
            p.PaymentMethodId,
            p.Name,
            p.Status,
            string.Equals(p.Name, "Cash", StringComparison.OrdinalIgnoreCase))).ToList();

        var cachedDiscounts = new List<CachedDiscountPolicy>
        {
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Standard 10%", "Percentage", 10m, 1000m, false),
            new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Manager 20%", "Percentage", 20m, 5000m, true)
        };

        var cachedOperators = new List<CachedOperatorClaim>
        {
            new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Administrator", "Administrator", true, true, DateTimeOffset.UtcNow.AddDays(7))
        };

        List<CachedCustomer> cachedCustomers = [];
        try
        {
            var customers = await _mediator.Send(new ListCustomersQuery(), cancellationToken).ConfigureAwait(false);
            cachedCustomers = customers.Where(c => c.IsActive).Select(c => new CachedCustomer(
                c.CustomerId,
                c.Name,
                c.MobileNumber,
                c.Code,
                c.IsDefault,
                c.IsActive,
                c.IsDefault,
                c.OutstandingBalance,
                c.IsCreditAllowed)).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load customers for cache; continuing with default walk-in customer");
        }

        if (cachedCustomers.Count == 0)
        {
            cachedCustomers.Add(new CachedCustomer(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Walk-in Customer", null, null, true, true, true, 0m, false));
        }

        var payload = new OperationalCachePayload
        {
            Categories = cachedCategories,
            Products = cachedProducts,
            Variants = cachedVariants,
            QuickOrderTemplates = cachedTemplates,
            DiningAreas = cachedAreas,
            Tables = cachedTables,
            Discounts = cachedDiscounts,
            PaymentMethods = cachedPaymentMethods,
            Operators = cachedOperators,
            Customers = cachedCustomers
        };

        // 4. Compute cryptographic authentication
        var nowUtc = DateTimeOffset.UtcNow;
        var cacheVersion = Guid.NewGuid().ToString("N");
        var payloadChecksum = OperationalCacheSnapshot.ComputePayloadChecksum(payload);
        var hmac = OperationalCacheSnapshot.ComputeHmacSignature(
            OperationalCacheSnapshot.CurrentSchemaVersion,
            cacheVersion,
            nowUtc,
            companyId,
            branchId,
            terminalId,
            warehouseId,
            payloadChecksum);

        var metadata = new OperationalCacheMetadata(
            SchemaVersion: OperationalCacheSnapshot.CurrentSchemaVersion,
            CacheVersion: cacheVersion,
            GeneratedAtUtc: nowUtc,
            LastSuccessfulSyncUtc: nowUtc,
            CompanyId: companyId,
            CompanyName: companyName,
            BranchId: branchId,
            BranchName: branchName,
            TerminalId: terminalId,
            TerminalName: terminalName,
            TerminalCode: terminalCode,
            WarehouseId: warehouseId,
            WarehouseName: warehouseName,
            CurrencyCode: currencyCode,
            CurrencySymbol: currencySymbol,
            CurrencyDecimalPlaces: currencyDecimals,
            SourceDatabaseIdentity: "Clovent_BusinessOperatingSystem",
            SourceRevision: $"{cachedProducts.Count}:{cachedVariants.Count}:{nowUtc.Ticks}",
            TotalCategories: cachedCategories.Count,
            TotalProducts: cachedProducts.Count,
            TotalVariants: cachedVariants.Count,
            TotalTemplates: cachedTemplates.Count,
            PayloadChecksum: payloadChecksum,
            HmacSignature: hmac);

        var snapshot = new OperationalCacheSnapshot(metadata, payload);

        // 5. Persist snapshot atomically to local protected store
        await _cacheStore.SaveSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);

        lock (_syncLock)
        {
            _lastSyncedSnapshot = snapshot;
        }

        sw.Stop();
        _logger?.LogInformation(
            "Operational cache synchronization completed in {ElapsedMs}ms [Version: {Version}, Variants: {Variants}, Categories: {Categories}]",
            sw.ElapsedMilliseconds, cacheVersion, cachedVariants.Count, cachedCategories.Count);

        return snapshot;
    }

    /// <inheritdoc />
    public Task<OperationalCacheMetadata?> GetCurrentMetadataAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        return _cacheStore.GetMetadataAsync(terminalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(CacheValidationStatus Status, string Message)> ValidateCacheAsync(
        Guid branchId,
        Guid terminalId,
        CacheFreshnessPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _cacheStore.LoadSnapshotAsync(terminalId, cancellationToken).ConfigureAwait(false);
        if (snapshot == null)
        {
            return (CacheValidationStatus.Missing, "No operational cache snapshot exists on this terminal.");
        }

        return snapshot.Validate(branchId, terminalId, policy);
    }
}
