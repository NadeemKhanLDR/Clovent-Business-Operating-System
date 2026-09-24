using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using Clovent.Restaurant.Application.SmartCombos;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.QuickOrderTemplates;
using Clovent.Restaurant.SmartCombos;
using Xunit;
namespace Clovent.Restaurant.Application.Tests.SmartCombos;
public class SmartComboHandlerTests
{
    private readonly Guid _warehouse=Guid.NewGuid(), _currency=Guid.NewGuid(), _a=Guid.NewGuid(), _b=Guid.NewGuid(), _product=Guid.NewGuid();
    private readonly FakeQuickOrderTemplateRepository _templates=new();
    private readonly FakeActivityLogEntryRepository _audit=new();
    private readonly Store _store=new();
    private readonly Access _access=new();
    private readonly SmartComboOptions _options=new();
    private SmartComboService Service(bool inactive=false, decimal? cost=null)
    {
        _access.Currency=_currency;
        _store.Baskets=Enumerable.Range(0,5).Select(_=>new ComboBasket(Guid.NewGuid(),new[]{_a,_b})).ToList();
        var mediator=new FakeMediator(request=>Task.FromResult<object?>(request switch {
            ListProductVariantsQuery => new[]{CatalogFakes.Variant(_a,_product,"Half",inactive?"Inactive":"Active"),CatalogFakes.Variant(_b,_product,"Full")},
            ListProductsQuery => new[]{CatalogFakes.Product(_product,"Meal")},
            ListActiveProductPricesByTypeQuery q => q.PriceType==PriceType.Cost && cost==null ? Array.Empty<ProductPriceDto>() : new[]{_a,_b}.Select(id=>new ProductPriceDto(Guid.NewGuid(),id,q.PriceType.ToString(),q.PriceType==PriceType.Cost ? cost!.Value : 100m,_currency,DateTimeOffset.UtcNow.AddDays(-1),"Active",DateTimeOffset.UtcNow)).ToArray(),
            _=>throw new NotSupportedException()
        }));
        return new(_store,_templates,mediator,_access,_options);
    }
    private string Signature=>SmartComboCalculator.Signature([_a,_b]);
    [Fact] public async Task ManagerConversionCreatesCanonicalTemplateAndAuditWithEditedName()
    {
        var service=Service(); var handler=new ConvertSmartComboCommandHandler(service,_access,_store,_templates,_audit,_options);
        var id=await handler.Handle(new(_warehouse,30,Signature,"Manager's Lunch",185m),default);
        var template=Assert.Single(await _templates.GetAllAsync()); Assert.Equal(id,template.Id.Value); Assert.Equal("Manager's Lunch",template.Name); Assert.Equal(_warehouse,template.WarehouseId);
        Assert.Equal(2,template.Items.Count); Assert.All(template.Items,x=>Assert.Equal(1m,x.Quantity)); Assert.Equal(185m,template.Items.Sum(x=>x.TemplateUnitPrice));
        Assert.Equal(id,_store.Decision!.TemplateId); Assert.Contains("Manager's Lunch",Assert.Single(_audit.GetAll()).Details);
        Assert.Empty((await service.AnalyzeAsync(_warehouse,30,default)).Opportunities);
    }
    [Fact] public async Task ActiveEquivalentDealSuppressedRegardlessOfOrder()
    {
        var service=Service(); var template=QuickOrderTemplate.Create("Existing"); template.AddItem(new(_b),1); template.AddItem(new(_a),1); _templates.Add(template);
        Assert.Empty((await service.AnalyzeAsync(_warehouse,30,default)).Opportunities);
    }
    [Fact] public async Task OtherWarehouseDealDoesNotSuppressLocalOpportunity()
    {
        var service=Service(); var template=QuickOrderTemplate.Create("Elsewhere"); template.ScopeToWarehouse(Guid.NewGuid()); template.AddItem(new(_b),1); template.AddItem(new(_a),1); _templates.Add(template);
        Assert.Single((await service.AnalyzeAsync(_warehouse,30,default)).Opportunities);
    }
    [Fact] public async Task InactiveVariantFilteredByService() => Assert.Empty((await Service(true).AnalyzeAsync(_warehouse,30,default)).Opportunities);
    [Fact] public async Task DismissalPersistsAndSuppressesReanalysis()
    {
        var service=Service(); await new DismissSmartComboCommandHandler(service,_access,_store,_audit,_options).Handle(new(_warehouse,30,Signature,"Margin too low"),default);
        Assert.Equal("Margin too low",_store.Decision!.Reason); Assert.Empty((await service.AnalyzeAsync(_warehouse,30,default)).Opportunities);
    }
    [Fact] public async Task ConversionRechecksMarginAndDoesNotCreateOnFailure()
    {
        var service=Service(cost:75m); await Assert.ThrowsAsync<ArgumentException>(()=>new ConvertSmartComboCommandHandler(service,_access,_store,_templates,_audit,_options).Handle(new(_warehouse,30,Signature,"Lunch",160m),default));
        Assert.Empty(await _templates.GetAllAsync()); Assert.Null(_store.Decision);
    }
    [Fact] public async Task DeniedConversionCannotPublish()
    {
        var service=Service(); _access.Deny=true;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new ConvertSmartComboCommandHandler(service,_access,_store,_templates,_audit,_options).Handle(new(_warehouse,30,Signature,"Lunch",190m),default));
        Assert.Empty(await _templates.GetAllAsync());
    }
    [Fact] public async Task AnalysisPassesWarehouseAndBoundedPeriodToStore()
    {
        var start=DateTimeOffset.UtcNow; await Service().AnalyzeAsync(_warehouse,30,default);
        Assert.Equal(_warehouse,_store.Warehouse); Assert.InRange(_store.From,start.AddDays(-30),DateTimeOffset.UtcNow.AddDays(-30)); Assert.Equal(TimeSpan.FromDays(30),_store.To-_store.From);
    }
    private sealed class Access:ISmartComboAccess
    {
        public Guid Currency; public bool Deny;
        public Task<Guid> RequireAsync(string operation,Guid warehouseId,CancellationToken ct) => Deny ? throw new UnauthorizedAccessException() : Task.FromResult(Guid.NewGuid());
        public Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct)=>Task.FromResult<IReadOnlyList<ComboLocation>>([]);
        public Task<ComboCurrency> CurrencyAsync(Guid warehouseId,CancellationToken ct)=>Task.FromResult(new ComboCurrency(Currency,2,"Rs."));
    }
    private sealed class Store:ISmartComboStore
    {
        public IReadOnlyList<ComboBasket> Baskets=[]; public ComboDecision? Decision; public Guid Warehouse; public DateTimeOffset From,To;
        public Task<IReadOnlyList<ComboBasket>> ReadBasketsAsync(Guid warehouseId,DateTimeOffset from,DateTimeOffset to,int maximumOrders,CancellationToken ct,bool includeDevelopmentSamples=false) { Warehouse=warehouseId;From=from;To=to;return Task.FromResult(Baskets); }
        public Task<IReadOnlyList<ComboDecision>> DecisionsAsync(Guid warehouseId,CancellationToken ct)=>Task.FromResult<IReadOnlyList<ComboDecision>>(Decision==null?[]:[Decision]);
        public Task<ComboDecision> DecisionAsync(Guid warehouseId,string signature,CancellationToken ct)=>Task.FromResult(Decision??=ComboDecision.Create(warehouseId,signature));
    }
}

