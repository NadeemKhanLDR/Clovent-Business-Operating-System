using Clovent.Restaurant.SmartCombos;
namespace Clovent.Restaurant.Application.SmartCombos;

public sealed record SmartComboOptions
{
    public bool IncludeDevelopmentSamples { get; init; } = false;
    public int PeriodDays { get; init; } = 30;
    public int MinimumOrders { get; init; } = 5;
    public int MinimumFrequency { get; init; } = 3;
    public decimal MinimumSupport { get; init; } = 0.01m;
    public decimal MinimumAttachRate { get; init; } = 0.10m;
    public decimal MinimumLift { get; init; } = 1m;
    public int MaximumSuggestions { get; init; } = 20;
    public bool IncludeTriples { get; init; } = true;
    public decimal DiscountRate { get; init; } = 0.05m;
    public decimal MinimumMargin { get; init; } = 0.20m;
    public int DismissalDays { get; init; } = 30;
    public int MaximumOrders { get; init; } = 50000;
    public int MaximumBasketSize { get; init; } = 40;
    public void Validate()
    {
        if (PeriodDays is < 1 or > 366 || MinimumOrders < 1 || MinimumFrequency < 1 ||
            MinimumSupport is < 0 or > 1 || MinimumAttachRate is < 0 or > 1 || MinimumLift < 0 ||
            MaximumSuggestions is < 1 or > 100 || DiscountRate is < 0 or >= 1 || MinimumMargin is < 0 or >= 1 ||
            DismissalDays is < 1 or > 365 || MaximumOrders is < 1 or > 100000 || MaximumBasketSize is < 3 or > 50)
            throw new ArgumentException("Invalid SmartCombos configuration.");
    }
}
public sealed record ComboItem(Guid VariantId, string ProductName, string VariantName, decimal Price, decimal? Cost, decimal NetRevenueFactor = 1m)
{
    public string DisplayName => $"{ProductName} - {VariantName}";
}
public sealed record ComboBasket(Guid OrderId, IReadOnlyCollection<Guid> Variants);
public sealed record ComboOpportunity(string Signature, string Name, IReadOnlyList<ComboItem> Items,
    int Frequency, int EligibleOrders, decimal Support, decimal AttachRate, decimal Lift,
    string Antecedent, string Consequent, decimal NormalPrice, decimal SuggestedPrice, decimal? Cost, int CurrencyDecimals = 2)
{
    public decimal DiscountRate => (NormalPrice - SuggestedPrice) / NormalPrice;
    public decimal NetRevenue(decimal price) => SmartComboCalculator.Allocate(this, price, CurrencyDecimals).Sum(x => x.TemplateUnitPrice!.Value * Items.Single(i => i.VariantId == x.VariantId).NetRevenueFactor);
    public decimal NormalNetRevenue => Items.Sum(x => x.Price * x.NetRevenueFactor);
    public decimal? GrossProfit => Cost is { } cost ? NetRevenue(SuggestedPrice) - cost : null;
    public decimal? Margin => GrossProfit / NetRevenue(SuggestedPrice);
}
public sealed record ComboAnalysis(Guid WarehouseId, DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int EligibleOrders, int OversizedBaskets, int ConvertedCount, IReadOnlyList<ComboOpportunity> Opportunities);
public sealed record ComboLocation(Guid Id, string Name);
public sealed record ComboCurrency(Guid Id, int DecimalPlaces, string Symbol);
public interface ISmartComboAccess
{
    Task<Guid> RequireAsync(string operation, Guid warehouseId, CancellationToken ct);
    Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct);
    Task<ComboCurrency> CurrencyAsync(Guid warehouseId, CancellationToken ct);
}
public interface ISmartComboStore
{
    Task<IReadOnlyList<ComboBasket>> ReadBasketsAsync(Guid warehouseId, DateTimeOffset from, DateTimeOffset to, int maximumOrders, CancellationToken ct, bool includeDevelopmentSamples = false);
    Task<IReadOnlyList<ComboDecision>> DecisionsAsync(Guid warehouseId, CancellationToken ct);
    Task<ComboDecision> DecisionAsync(Guid warehouseId, string signature, CancellationToken ct);
}



