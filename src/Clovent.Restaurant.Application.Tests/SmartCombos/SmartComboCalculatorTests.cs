using Clovent.Restaurant.Application.SmartCombos;
using Clovent.Restaurant.SmartCombos;
using Xunit;
namespace Clovent.Restaurant.Application.Tests.SmartCombos;
public class SmartComboCalculatorTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-000000000001"), B = Guid.Parse("00000000-0000-0000-0000-000000000002"), C = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly SmartComboOptions Options = new() { MinimumFrequency = 1, MinimumOrders = 1, MinimumSupport = 0, MinimumAttachRate = 0, MinimumLift = 0 };
    private static Dictionary<Guid, ComboItem> Catalog => new[] { A, B, C }.ToDictionary(x => x, x => new ComboItem(x, "Product", "Standard", 100m, null));
    private static ComboBasket Basket(params Guid[] ids) => new(Guid.NewGuid(), ids);
    private static List<ComboBasket> Baskets => [Basket(A,B,C), Basket(A,B,C), Basket(A,B), Basket(A), Basket(C)];
    [Fact] public void Pairs_CountOrderPresenceAndCalculateSupportAttachLift()
    {
        var pair = SmartComboCalculator.Analyze(Baskets, Catalog, Options, 2, new HashSet<string>()).Single(x => x.Signature == SmartComboCalculator.Signature([A,B]));
        Assert.Equal(3, pair.Frequency); Assert.Equal(0.6m, pair.Support); Assert.Equal(1m, pair.AttachRate); Assert.Equal(1.25m, pair.Lift);
    }
    [Fact] public void Triples_UsePairAntecedentAndSingletonConsequent()
    {
        var triple = SmartComboCalculator.Analyze(Baskets, Catalog, Options, 2, new HashSet<string>()).Single(x => x.Items.Count == 3);
        Assert.Equal(2, triple.Frequency); Assert.Equal(0.4m, triple.Support); Assert.Equal(1m, triple.AttachRate); Assert.Equal(5m/3m, triple.Lift);
    }
    [Fact] public void Signature_IsOrderIndependentAndDeduplicated() => Assert.Equal(SmartComboCalculator.Signature([A,B,C]), SmartComboCalculator.Signature([C,B,A,A]));
    [Fact] public void DuplicateLines_DoNotIncreaseFrequency()
    {
        var rows = SmartComboCalculator.Analyze([Basket(A,A,B,B)], Catalog, Options, 2, new HashSet<string>());
        Assert.Equal(1, Assert.Single(rows).Frequency);
    }
    [Fact] public void IneligibleCatalogIdentityExcluded() => Assert.Empty(SmartComboCalculator.Analyze([Basket(A,B)], new Dictionary<Guid,ComboItem> { [A] = Catalog[A] }, Options, 2, new HashSet<string>()));
    [Fact] public void SuppressedSignatureExcluded() => Assert.Empty(SmartComboCalculator.Analyze([Basket(A,B)], Catalog, Options, 2, new HashSet<string> { SmartComboCalculator.Signature([A,B]) }));
    [Fact] public void PairOnlyDoesNotProduceTriples() => Assert.All(SmartComboCalculator.Analyze(Baskets, Catalog, Options with { IncludeTriples = false }, 2, new HashSet<string>()), x => Assert.Equal(2,x.Items.Count));
    [Fact] public void ThresholdsApplied() => Assert.Empty(SmartComboCalculator.Analyze(Baskets,Catalog,Options with { MinimumFrequency = 4 },2,new HashSet<string>()));
    [Theory] [InlineData(530d, null, 503.5d)] [InlineData(530d, 420d, 525d)] [InlineData(530d, 425d, null)]
    public void Price_DiscountMarginAndUnavailableCost(double normal, double? cost, double? expected) => Assert.Equal(expected.HasValue ? (decimal)expected.Value : null,SmartComboCalculator.Price((decimal)normal,cost.HasValue ? (decimal)cost.Value : null,new(),2));
    [Fact] public void MarginFloorRoundsUp() => Assert.Equal(125.02m,SmartComboCalculator.Price(130m,100.01m,new(),2));
    [Fact] public void MissingCostNeverFabricatesMargin()
    {
        var x=Assert.Single(SmartComboCalculator.Analyze([Basket(A,B)],Catalog,Options,2,new HashSet<string>()));
        Assert.Null(x.Cost); Assert.Null(x.Margin); Assert.Null(x.GrossProfit);
    }
    [Fact] public void AllocationPreservesExactTotalAndOneOfEach()
    {
        var x=Assert.Single(SmartComboCalculator.Analyze([Basket(A,B)],Catalog,Options,2,new HashSet<string>()));
        var items=SmartComboCalculator.Allocate(x,177.77m,2);
        Assert.Equal(177.77m,items.Sum(i=>i.TemplateUnitPrice)); Assert.All(items,i=>Assert.Equal(1m,i.Quantity));
    }
    [Fact] public void DismissalExpiresAfterConfiguredPeriod()
    {
        var d=ComboDecision.Create(Guid.NewGuid(),SmartComboCalculator.Signature([A,B])); d.Dismiss(Guid.NewGuid(),"Seasonal",30);
        Assert.True(d.Suppresses(DateTimeOffset.UtcNow)); Assert.False(d.Suppresses(DateTimeOffset.UtcNow.AddDays(31)));
        Assert.Equal("Seasonal",d.Reason);
    }
    [Fact] public void ConvertedDecisionNeverReappears()
    {
        var d=ComboDecision.Create(Guid.NewGuid(),SmartComboCalculator.Signature([A,B])); var id=Guid.NewGuid(); d.Convert(Guid.NewGuid(),id);
        Assert.Equal(id,d.TemplateId); Assert.True(d.Suppresses(DateTimeOffset.UtcNow.AddYears(1)));
        Assert.Throws<InvalidOperationException>(()=>d.Convert(Guid.NewGuid(),Guid.NewGuid()));
    }
    [Fact] public void TaxInclusiveRevenueProtectsMargin() => Assert.InRange(SmartComboCalculator.Price(155m,100m,new(),2,1m/1.2m)!.Value,150m,150.01m);
    [Fact] public void InvalidConfigurationRejected() => Assert.Throws<ArgumentException>(() => (Options with { MinimumMargin=1 }).Validate());
}




