using Clovent.Restaurant.SmartCombos;
namespace Clovent.Restaurant.Application.SmartCombos;

/// <summary>Configuration options for Smart Combo basket mining and deal pricing.</summary>
public sealed record SmartComboOptions
{
    /// <summary>Whether development sample baskets are included in analysis.</summary>
    public bool IncludeDevelopmentSamples { get; init; } = false;

    /// <summary>Default analysis window in days.</summary>
    public int PeriodDays { get; init; } = 30;

    /// <summary>Minimum eligible orders required before suggesting combos.</summary>
    public int MinimumOrders { get; init; } = 5;

    /// <summary>Minimum co-occurrence frequency for a combo candidate.</summary>
    public int MinimumFrequency { get; init; } = 3;

    /// <summary>Minimum support threshold across eligible baskets.</summary>
    public decimal MinimumSupport { get; init; } = 0.01m;

    /// <summary>Minimum attach rate (confidence) threshold.</summary>
    public decimal MinimumAttachRate { get; init; } = 0.10m;

    /// <summary>Minimum association lift threshold.</summary>
    public decimal MinimumLift { get; init; } = 1m;

    /// <summary>Maximum number of combo suggestions returned per analysis.</summary>
    public int MaximumSuggestions { get; init; } = 20;

    /// <summary>Whether three-item combo candidates are evaluated in addition to pairs.</summary>
    public bool IncludeTriples { get; init; } = true;

    /// <summary>Default promotional discount rate applied to normal price.</summary>
    public decimal DiscountRate { get; init; } = 0.05m;

    /// <summary>Minimum gross margin floor enforced on suggested combo prices.</summary>
    public decimal MinimumMargin { get; init; } = 0.20m;

    /// <summary>Number of days a dismissed combo remains suppressed.</summary>
    public int DismissalDays { get; init; } = 30;

    /// <summary>Maximum number of orders scanned in a single analysis request.</summary>
    public int MaximumOrders { get; init; } = 50000;

    /// <summary>Maximum distinct catalog variants per basket included in mining.</summary>
    public int MaximumBasketSize { get; init; } = 40;

    /// <summary>Validates that all Smart Combo option thresholds are within supported bounds.</summary>
    public void Validate()
    {
        if (PeriodDays is < 1 or > 366 || MinimumOrders < 1 || MinimumFrequency < 1 ||
            MinimumSupport is < 0 or > 1 || MinimumAttachRate is < 0 or > 1 || MinimumLift < 0 ||
            MaximumSuggestions is < 1 or > 100 || DiscountRate is < 0 or >= 1 || MinimumMargin is < 0 or >= 1 ||
            DismissalDays is < 1 or > 365 || MaximumOrders is < 1 or > 100000 || MaximumBasketSize is < 3 or > 50)
            throw new ArgumentException("Invalid SmartCombos configuration.");
    }
}

/// <summary>Catalog variant metadata and pricing used in Smart Combo analysis.</summary>
/// <param name="VariantId">Product variant identifier.</param>
/// <param name="ProductName">Parent product name.</param>
/// <param name="VariantName">Portion or variant name.</param>
/// <param name="Price">Active selling unit price.</param>
/// <param name="Cost">Optional active unit cost.</param>
/// <param name="NetRevenueFactor">Factor converting selling price to net tax-exclusive revenue.</param>
public sealed record ComboItem(Guid VariantId, string ProductName, string VariantName, decimal Price, decimal? Cost, decimal NetRevenueFactor = 1m)
{
    /// <summary>Formatted display name combining product and variant names.</summary>
    public string DisplayName => $"{ProductName} - {VariantName}";
}

/// <summary>Completed order basket containing distinct product variants.</summary>
/// <param name="OrderId">Completed order identifier.</param>
/// <param name="Variants">Distinct product variant identifiers present in the order.</param>
public sealed record ComboBasket(Guid OrderId, IReadOnlyCollection<Guid> Variants);

/// <summary>Discovered Smart Combo opportunity with association metrics and suggested deal pricing.</summary>
/// <param name="Signature">Canonical sorted variant signature.</param>
/// <param name="Name">Suggested combo display name.</param>
/// <param name="Items">Component items in the combo.</param>
/// <param name="Frequency">Number of orders containing all combo items together.</param>
/// <param name="EligibleOrders">Total eligible orders analyzed.</param>
/// <param name="Support">Support ratio across eligible orders.</param>
/// <param name="AttachRate">Attach rate (confidence) from antecedent to consequent.</param>
/// <param name="Lift">Association lift score.</param>
/// <param name="Antecedent">Antecedent item display label.</param>
/// <param name="Consequent">Consequent item display label.</param>
/// <param name="NormalPrice">Sum of regular selling prices of component items.</param>
/// <param name="SuggestedPrice">Suggested promotional combo price.</param>
/// <param name="Cost">Combined unit cost when known for all items.</param>
/// <param name="CurrencyDecimals">Number of currency decimal places.</param>
public sealed record ComboOpportunity(string Signature, string Name, IReadOnlyList<ComboItem> Items,
    int Frequency, int EligibleOrders, decimal Support, decimal AttachRate, decimal Lift,
    string Antecedent, string Consequent, decimal NormalPrice, decimal SuggestedPrice, decimal? Cost, int CurrencyDecimals = 2)
{
    /// <summary>Effective discount fraction relative to <see cref="NormalPrice"/>.</summary>
    public decimal DiscountRate => (NormalPrice - SuggestedPrice) / NormalPrice;

    /// <summary>Computes net tax-exclusive revenue for a given deal <paramref name="price"/>.</summary>
    public decimal NetRevenue(decimal price) => SmartComboCalculator.Allocate(this, price, CurrencyDecimals).Sum(x => x.TemplateUnitPrice!.Value * Items.Single(i => i.VariantId == x.VariantId).NetRevenueFactor);

    /// <summary>Normal tax-exclusive revenue across all component items.</summary>
    public decimal NormalNetRevenue => Items.Sum(x => x.Price * x.NetRevenueFactor);

    /// <summary>Estimated gross profit at <see cref="SuggestedPrice"/> when item costs are available.</summary>
    public decimal? GrossProfit => Cost is { } cost ? NetRevenue(SuggestedPrice) - cost : null;

    /// <summary>Estimated gross margin ratio at <see cref="SuggestedPrice"/> when item costs are available.</summary>
    public decimal? Margin => GrossProfit / NetRevenue(SuggestedPrice);
}

/// <summary>Result of analyzing completed baskets for a warehouse over a date range.</summary>
/// <param name="WarehouseId">Analyzed warehouse identifier.</param>
/// <param name="FromUtc">Inclusive start of analysis window in UTC.</param>
/// <param name="ToUtc">Exclusive end of analysis window in UTC.</param>
/// <param name="EligibleOrders">Count of eligible baskets analyzed.</param>
/// <param name="OversizedBaskets">Count of baskets excluded for exceeding maximum basket size.</param>
/// <param name="ConvertedCount">Count of previously converted combo decisions.</param>
/// <param name="Opportunities">Ranked list of discovered combo opportunities.</param>
public sealed record ComboAnalysis(Guid WarehouseId, DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int EligibleOrders, int OversizedBaskets, int ConvertedCount, IReadOnlyList<ComboOpportunity> Opportunities);

/// <summary>Warehouse location option available for Smart Combo analysis.</summary>
/// <param name="Id">Warehouse identifier.</param>
/// <param name="Name">Warehouse display name.</param>
public sealed record ComboLocation(Guid Id, string Name);

/// <summary>Currency formatting metadata for Smart Combo pricing.</summary>
/// <param name="Id">Currency identifier.</param>
/// <param name="DecimalPlaces">Number of currency decimal places.</param>
/// <param name="Symbol">Currency display symbol.</param>
public sealed record ComboCurrency(Guid Id, int DecimalPlaces, string Symbol);

/// <summary>Access-control and context resolution service for Smart Combo operations.</summary>
public interface ISmartComboAccess
{
    /// <summary>Verifies authorization for the specified Smart Combo <paramref name="operation"/> and returns the acting user ID.</summary>
    Task<Guid> RequireAsync(string operation, Guid warehouseId, CancellationToken ct);

    /// <summary>Lists warehouse locations accessible to the current user.</summary>
    Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct);

    /// <summary>Resolves the active currency metadata for the specified warehouse.</summary>
    Task<ComboCurrency> CurrencyAsync(Guid warehouseId, CancellationToken ct);
}

/// <summary>Persistence store for reading order baskets and recording manager combo decisions.</summary>
public interface ISmartComboStore
{
    /// <summary>Reads completed order baskets for the specified warehouse and UTC date window.</summary>
    Task<IReadOnlyList<ComboBasket>> ReadBasketsAsync(Guid warehouseId, DateTimeOffset from, DateTimeOffset to, int maximumOrders, CancellationToken ct, bool includeDevelopmentSamples = false);

    /// <summary>Returns all persisted combo decisions for the specified warehouse.</summary>
    Task<IReadOnlyList<ComboDecision>> DecisionsAsync(Guid warehouseId, CancellationToken ct);

    /// <summary>Loads or creates a tracked <see cref="ComboDecision"/> for the specified warehouse and combo signature.</summary>
    Task<ComboDecision> DecisionAsync(Guid warehouseId, string signature, CancellationToken ct);
}



