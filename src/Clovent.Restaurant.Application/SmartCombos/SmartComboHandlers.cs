using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;
namespace Clovent.Restaurant.Application.SmartCombos;

public sealed record AnalyzeSmartCombosQuery(Guid WarehouseId, int PeriodDays = 30) : IRequest<ComboAnalysis>;
public sealed class AnalyzeSmartCombosQueryHandler(SmartComboService service, ISmartComboAccess access,
    IActivityLogEntryRepository audit) : IRequestHandler<AnalyzeSmartCombosQuery, ComboAnalysis>
{
    public async Task<ComboAnalysis> Handle(AnalyzeSmartCombosQuery request, CancellationToken ct)
    {
        var user = await access.RequireAsync("analyze", request.WarehouseId, ct);
        var result = await service.AnalyzeAsync(request.WarehouseId, request.PeriodDays, ct);
        await audit.AddAsync(ActivityLogEntry.Record("Smart Combo Analysis", $"Warehouse {request.WarehouseId}; {result.EligibleOrders} eligible orders; {request.PeriodDays} days", user.ToString(), Environment.MachineName), ct);
        return result;
    }
}
public sealed class SmartComboService(ISmartComboStore store, IQuickOrderTemplateRepository templates,
    IMediator mediator, ISmartComboAccess access, SmartComboOptions options)
{
    public async Task<ComboAnalysis> AnalyzeAsync(Guid warehouseId, int days, CancellationToken ct)
    {
        options.Validate();
        if (days is < 1 or > 366) throw new ArgumentException("Period must be 1 to 366 days.");
        var now = DateTimeOffset.UtcNow;
        var currency = await access.CurrencyAsync(warehouseId, ct);
        var baskets = await store.ReadBasketsAsync(warehouseId, now.AddDays(-days), now, options.MaximumOrders, ct, options.IncludeDevelopmentSamples);
        var variants = await mediator.Send(new ListProductVariantsQuery(), ct);
        var products = (await mediator.Send(new ListProductsQuery(), ct)).Where(x => x.Status == "Active" && !x.Name.StartsWith("QA ", StringComparison.OrdinalIgnoreCase) && !x.Name.StartsWith("TEST ", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x.ProductId);
        var selling = (await mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Selling), ct))
            .Where(x => x.CurrencyId == currency.Id && x.EffectiveFromUtc <= now).GroupBy(x => x.ProductVariantId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.EffectiveFromUtc).ThenBy(p => p.ProductPriceId).First().Amount);
        var costs = (await mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Cost), ct))
            .Where(x => x.CurrencyId == currency.Id && x.EffectiveFromUtc <= now).GroupBy(x => x.ProductVariantId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.EffectiveFromUtc).ThenBy(p => p.ProductPriceId).First().Amount);
        var catalog = variants.Where(x => x.Status == "Active" && products.ContainsKey(x.ProductId) && selling.GetValueOrDefault(x.ProductVariantId) > 0)
            .ToDictionary(x => x.ProductVariantId, x => new ComboItem(x.ProductVariantId, products[x.ProductId].Name, x.Name,
                selling[x.ProductVariantId], costs.TryGetValue(x.ProductVariantId, out var cost) && cost >= 0 ? cost : null,
                products[x.ProductId].TaxIsInclusive ? 1m / (1m + products[x.ProductId].TaxRatePercentage / 100m) : 1m));
        var decisions = await store.DecisionsAsync(warehouseId, ct);
        var suppressed = decisions.Where(x => x.Suppresses(now)).Select(x => x.Signature).ToHashSet();
        foreach (var template in await templates.GetActiveAsync(ct))
            if ((template.WarehouseId == null || template.WarehouseId == warehouseId) && template.Items.All(x => x.Quantity == 1m))
                suppressed.Add(SmartComboCalculator.Signature(template.Items.Select(x => x.VariantId.Value)));
        // CPU work stays off the WinForms synchronization context.
        var opportunities = await Task.Run(() => SmartComboCalculator.Analyze(baskets, catalog, options, currency.DecimalPlaces, suppressed, ct), ct);
        return new(warehouseId, now.AddDays(-days), now, baskets.Count(b => b.Variants.Count(catalog.ContainsKey) <= options.MaximumBasketSize),
            baskets.Count(b => b.Variants.Count(catalog.ContainsKey) > options.MaximumBasketSize),
            decisions.Count(x => x.TemplateId != null), opportunities);
    }
}
public sealed record ConvertSmartComboCommand(Guid WarehouseId, int PeriodDays, string Signature, string Name, decimal Price) : IRequest<Guid>;
public sealed class ConvertSmartComboCommandHandler(SmartComboService service, ISmartComboAccess access,
    ISmartComboStore store, IQuickOrderTemplateRepository templates, IActivityLogEntryRepository audit,
    SmartComboOptions options) : IRequestHandler<ConvertSmartComboCommand, Guid>
{
    public async Task<Guid> Handle(ConvertSmartComboCommand request, CancellationToken ct)
    {
        var user = await access.RequireAsync("create", request.WarehouseId, ct);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100) throw new ArgumentException("Enter a name of 1–100 characters.");
        var analysis = await service.AnalyzeAsync(request.WarehouseId, request.PeriodDays, ct);
        var opportunity = analysis.Opportunities.SingleOrDefault(x => x.Signature == request.Signature)
            ?? throw new InvalidOperationException("This opportunity changed or was already handled. Analyze sales again.");
        var currency = await access.CurrencyAsync(request.WarehouseId, ct);
        if (request.Price <= 0 || request.Price > opportunity.NormalPrice || Math.Round(request.Price, currency.DecimalPlaces) != request.Price ||
            opportunity.Cost is { } cost && (opportunity.NetRevenue(request.Price) - cost) / opportunity.NetRevenue(request.Price) < options.MinimumMargin)
            throw new ArgumentException("Deal price must respect currency precision, normal price and minimum margin.");
        // Invoke the canonical handler directly to stage its aggregate WITHOUT an inner UoW commit.
        // Template + decision + audit are committed by the outer Restaurant UoW in one SaveChanges transaction.
        var decision = await store.DecisionAsync(request.WarehouseId, request.Signature, ct);
        if (decision.Suppresses(DateTimeOffset.UtcNow)) throw new InvalidOperationException("Opportunity was already handled.");
        var id = await new CreateQuickOrderTemplateCommandHandler(templates).Handle(new(
            request.Name.Trim(), $"Smart Combo: {opportunity.Frequency}/{opportunity.EligibleOrders} orders over {request.PeriodDays} days.", 0,
            SmartComboCalculator.Allocate(opportunity, request.Price, currency.DecimalPlaces), request.WarehouseId), ct);
        decision.Convert(user, id);
        await audit.AddAsync(ActivityLogEntry.Record("Smart Combo Converted",
            $"Warehouse {request.WarehouseId}; Template {id}; {request.Signature}; Name '{opportunity.Name}' -> '{request.Name.Trim()}'; Price {opportunity.SuggestedPrice} -> {request.Price}", user.ToString(), Environment.MachineName), ct);
        return id;
    }
}
public sealed record DismissSmartComboCommand(Guid WarehouseId, int PeriodDays, string Signature, string? Reason) : IRequest;
public sealed class DismissSmartComboCommandHandler(SmartComboService service, ISmartComboAccess access,
    ISmartComboStore store, IActivityLogEntryRepository audit, SmartComboOptions options) : IRequestHandler<DismissSmartComboCommand>
{
    public async Task Handle(DismissSmartComboCommand request, CancellationToken ct)
    {
        var user = await access.RequireAsync("dismiss", request.WarehouseId, ct);
        var analysis = await service.AnalyzeAsync(request.WarehouseId, request.PeriodDays, ct);
        if (!analysis.Opportunities.Any(x => x.Signature == request.Signature)) throw new InvalidOperationException("Opportunity changed. Analyze again.");
        var decision = await store.DecisionAsync(request.WarehouseId, request.Signature, ct);
        decision.Dismiss(user, request.Reason, options.DismissalDays);
        await audit.AddAsync(ActivityLogEntry.Record("Smart Combo Dismissed", $"Warehouse {request.WarehouseId}; {request.Signature}; Until {decision.DismissedUntilUtc:u}; {request.Reason}", user.ToString(), Environment.MachineName), ct);
    }
}



