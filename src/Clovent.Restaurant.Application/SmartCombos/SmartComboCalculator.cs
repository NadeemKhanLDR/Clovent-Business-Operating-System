namespace Clovent.Restaurant.Application.SmartCombos;

/// <summary>Order-presence basket mining. Triples use a two-item antecedent and one-item consequent.</summary>
public static class SmartComboCalculator
{
    public static string Signature(IEnumerable<Guid> ids) => string.Join("|", ids.Distinct().Order().Select(x => x.ToString("N")));
    public static decimal? Price(decimal normal, decimal? cost, SmartComboOptions options, int decimals, decimal revenueFactor = 1m)
    {
        options.Validate();
        if (normal <= 0 || cost < 0 || revenueFactor is <= 0 or > 1 || decimals is < 0 or > 4) return null;
        var factor = (decimal)Math.Pow(10, decimals);
        var floor = cost is { } c ? Math.Ceiling(c / ((1 - options.MinimumMargin) * revenueFactor) * factor) / factor : 0m;
        var price = Math.Max(Math.Round(normal * (1 - options.DiscountRate), decimals, MidpointRounding.AwayFromZero), floor);
        return price > 0 && price <= normal ? price : null;
    }
    public static IReadOnlyList<ComboOpportunity> Analyze(IReadOnlyList<ComboBasket> baskets,
        IReadOnlyDictionary<Guid, ComboItem> catalog, SmartComboOptions options, int decimals, ISet<string> suppressed,
        CancellationToken ct = default)
    {
        options.Validate();
        baskets = baskets.Where(b => b.Variants.Count(catalog.ContainsKey) <= options.MaximumBasketSize).ToList();
        if (baskets.Count < options.MinimumOrders) return [];
        var counts = new Dictionary<string, (Guid[] Ids, int Count)>();
        void Count(Guid[] ids)
        {
            var key = Signature(ids);
            if (counts.Count >= 250000 && !counts.ContainsKey(key)) throw new InvalidOperationException("Too many distinct combinations. Use a shorter period or pairs only.");
            counts[key] = (ids, counts.TryGetValue(key, out var old) ? old.Count + 1 : 1);
        }
        foreach (var basket in baskets)
        {
            ct.ThrowIfCancellationRequested();
            var ids = basket.Variants.Where(catalog.ContainsKey).Distinct().Order().ToArray();
            if (ids.Length > options.MaximumBasketSize) continue;
            for (int i = 0; i < ids.Length; i++)
            {
                Count([ids[i]]);
                for (int j = i + 1; j < ids.Length; j++)
                {
                    Count([ids[i], ids[j]]);
                    if (options.IncludeTriples)
                        for (int k = j + 1; k < ids.Length; k++) Count([ids[i], ids[j], ids[k]]);
                }
            }
        }
        var results = new List<ComboOpportunity>();
        foreach (var (key, entry) in counts)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Ids.Length < 2 || entry.Count < options.MinimumFrequency || suppressed.Contains(key)) continue;
            var support = (decimal)entry.Count / baskets.Count;
            if (support < options.MinimumSupport) continue;
            var directions = entry.Ids.Select(consequent =>
            {
                var antecedent = entry.Ids.Where(x => x != consequent).ToArray();
                var attach = (decimal)entry.Count / counts[Signature(antecedent)].Count;
                var lift = attach / ((decimal)counts[Signature([consequent])].Count / baskets.Count);
                return (antecedent, consequent, attach, lift);
            }).Where(x => x.attach >= options.MinimumAttachRate && x.lift >= options.MinimumLift)
              .OrderByDescending(x => x.lift).ThenByDescending(x => x.attach).ThenBy(x => x.consequent).ToList();
            if (directions.Count == 0) continue;
            var direction = directions[0];
            var items = entry.Ids.Select(x => catalog[x]).OrderByDescending(x => x.Price).ThenBy(x => x.VariantId).ToList();
            var normal = items.Sum(x => x.Price);
            decimal? cost = items.All(x => x.Cost.HasValue) ? items.Sum(x => x.Cost!.Value) : null;
            var price = Price(normal, cost, options, decimals, items.Min(x => x.NetRevenueFactor));
            if (price == null) continue;
            results.Add(new(key, $"{items[0].ProductName} Combo", items, entry.Count, baskets.Count, support,
                direction.attach, direction.lift, string.Join(" + ", direction.antecedent.Select(x => catalog[x].DisplayName)),
                catalog[direction.consequent].DisplayName, normal, price.Value, cost, decimals));
        }
        return results.OrderByDescending(x => x.Frequency).ThenByDescending(x => x.Lift)
            .ThenBy(x => x.Signature, StringComparer.Ordinal).Take(options.MaximumSuggestions).ToList();
    }
    public static IReadOnlyList<QuickOrderTemplateItemInput> Allocate(ComboOpportunity opportunity, decimal price, int decimals)
    {
        var result = new List<QuickOrderTemplateItemInput>();
        var remaining = price;
        foreach (var item in opportunity.Items.SkipLast(1))
        {
            var amount = Math.Round(price * item.Price / opportunity.NormalPrice, decimals, MidpointRounding.ToZero);
            result.Add(new(item.VariantId, 1m, amount)); remaining -= amount;
        }
        result.Add(new(opportunity.Items[^1].VariantId, 1m, remaining));
        return result;
    }
}


