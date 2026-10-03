using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.EndOfDay.Queries;

/// <summary>Query to produce the comprehensive Expanded Sales Summary Report.</summary>
public sealed record GetExpandedSalesSummaryQuery(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<ExpandedSalesSummaryDto>;

/// <summary>Handles <see cref="GetExpandedSalesSummaryQuery"/>.</summary>
public sealed class GetExpandedSalesSummaryQueryHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IDiscountRepository discountRepository,
    IServiceChargeRepository serviceChargeRepository,
    IShiftRepository shiftRepository,
    ITableRepository tableRepository,
    IMediator mediator,
    Clovent.Restaurant.Application.Shifts.Services.IBusinessDateProvider? businessDateProvider = null) : IRequestHandler<GetExpandedSalesSummaryQuery, ExpandedSalesSummaryDto>
{
    private const string CashMethodName = "Cash";
    private const string CardFragment = "card";

    /// <inheritdoc/>
    public async Task<ExpandedSalesSummaryDto> Handle(GetExpandedSalesSummaryQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var allOrders = await orderRepository.GetAllAsync(cancellationToken);

        DateOnly ToBusinessDate(DateTimeOffset utc) =>
            businessDateProvider != null ? businessDateProvider.GetBusinessDateForUtc(utc) : DateOnly.FromDateTime(utc.LocalDateTime);

        bool MatchesRange(Order order)
        {
            if (order.WarehouseId != warehouseId) return false;
            // Check completed/updated or created date against business range
            var orderDate = ToBusinessDate(order.UpdatedAtUtc);
            var createdDate = ToBusinessDate(order.CreatedAtUtc);
            return (orderDate >= request.FromDate && orderDate <= request.ToDate) ||
                   (createdDate >= request.FromDate && createdDate <= request.ToDate);
        }

        var completedOrders = allOrders
            .Where(o => o.Status == OrderStatus.Completed && MatchesRange(o))
            .OrderByDescending(o => o.UpdatedAtUtc)
            .ToList();

        var voidedOrders = allOrders
            .Where(o => o.Status == OrderStatus.Voided && MatchesRange(o))
            .ToList();

        // Reference data lookups (safe against duplicate keys)
        var paymentMethods = (await paymentMethodRepository.GetAllAsync(cancellationToken))
            .GroupBy(m => m.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var customers = (await customerRepository.GetAllAsync(cancellationToken))
            .GroupBy(c => c.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var tables = (await tableRepository.GetAllAsync(cancellationToken))
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First());

        // Catalog lookups via Mediator (safely grouped to prevent duplicate key crashes)
        var variantList = await mediator.Send(new ListProductVariantsQuery(), cancellationToken) ?? [];
        var variants = variantList.GroupBy(v => v.ProductVariantId).ToDictionary(g => g.Key, g => g.First());

        var productList = await mediator.Send(new ListProductsQuery(), cancellationToken) ?? [];
        var products = productList.GroupBy(p => p.ProductId).ToDictionary(g => g.Key, g => g.First());

        var categoryList = await mediator.Send(new ListProductCategoriesQuery(), cancellationToken) ?? [];
        var categories = categoryList.GroupBy(c => c.ProductCategoryId).ToDictionary(g => g.Key, g => g.First());

        var costPricesList = await mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Cost), cancellationToken) ?? [];
        var costPrices = costPricesList
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);

        var sellingPricesList = await mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Selling), cancellationToken) ?? [];
        var sellingPrices = sellingPricesList
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);

        // Identify on-account and advance method IDs
        var onAccountMethodIds = paymentMethods.Values
            .Where(m => string.Equals(m.Name.Value, "On Account", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Customer Account", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Credit", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Customer Credit", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToHashSet();

        var advanceMethodIds = paymentMethods.Values
            .Where(m => string.Equals(m.Name.Value, "Customer Advance", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Advance", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToHashSet();

        // Identify default walk-in customer
        var defaultCustomer = customers.Values.FirstOrDefault(c => c.IsDefault || c.Code.Value == "C000");

        // Collect per-order details
        var orderRows = new List<ExpandedOrderRowDto>();
        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;
        decimal totalServiceCharges = 0m;
        decimal totalDeliveryFees = 0m;
        decimal totalTax = 0m;
        decimal totalCollected = 0m;
        decimal cashCollected = 0m;
        decimal cardCollected = 0m;
        decimal otherCollected = 0m;
        decimal onAccountCreated = 0m;
        decimal periodAdvancesApplied = 0m;
        decimal deliverySales = 0m;

        var variantAggregates = new Dictionary<Guid, (decimal Quantity, decimal GrossSales, decimal Discount, decimal NetSales, decimal Cost)>();
        var paymentMethodTotals = new Dictionary<string, (int Count, decimal Total, string Category)>();
        var customerSales = new Dictionary<Guid, (int Count, decimal Qty, decimal ItemSales, decimal Disc, decimal Fees, decimal Tax, decimal BillTotal, decimal Paid, decimal OnAccount)>();

        foreach (var order in completedOrders)
        {
            var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var activeLines = lines.Where(l => !l.IsVoided && l.Quantity > 0).ToList();

            var discounts = await discountRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var serviceCharges = await serviceChargeRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var activePayments = payments.Where(p => !p.IsVoided).ToList();

            decimal orderSubtotal = activeLines.Sum(l => l.LineTotal);
            decimal orderDiscount = discounts.Sum(d => d.DiscountType == DiscountType.Percentage ? orderSubtotal * d.Value / 100m : d.Value);
            decimal orderDeliveryFee = order.DeliveryFee;
            decimal orderOtherServiceCharges = serviceCharges
                .Where(sc => !string.Equals(sc.Reason, "Delivery Fee", StringComparison.OrdinalIgnoreCase))
                .Sum(sc => sc.ServiceChargeType == ServiceChargeType.Percentage ? orderSubtotal * sc.Value / 100m : sc.Value);
            decimal orderServiceAndDelivery = orderDeliveryFee + orderOtherServiceCharges;
            decimal orderTax = activeLines.Sum(l => l.TaxRatePercentage <= 0 ? 0m : (l.TaxIsInclusive ? l.LineTotal - l.LineTotal / (1 + l.TaxRatePercentage / 100m) : l.LineTotal * l.TaxRatePercentage / 100m));
            decimal orderExclusiveTax = activeLines.Where(l => !l.TaxIsInclusive).Sum(l => l.TaxRatePercentage <= 0 ? 0m : l.LineTotal * l.TaxRatePercentage / 100m);
            decimal orderBillTotal = Math.Max(0m, orderSubtotal - orderDiscount + orderServiceAndDelivery + orderExclusiveTax);
            decimal orderPaymentsTotal = activePayments.Sum(p => p.Amount);
            decimal orderTotal = orderBillTotal > 0 ? orderBillTotal : orderPaymentsTotal;

            grossSales += orderSubtotal;
            totalDiscounts += orderDiscount;
            totalDeliveryFees += orderDeliveryFee;
            totalServiceCharges += orderOtherServiceCharges;
            totalTax += orderTax;

            if (order.OrderType == OrderType.Delivery)
            {
                deliverySales += orderTotal;
            }

            // Resolve table/delivery
            string tableOrDelivery;
            if (order.OrderType == OrderType.DineIn)
            {
                tableOrDelivery = order.TableId.HasValue && tables.TryGetValue(order.TableId.Value, out var tbl)
                    ? $"Table {tbl.Code.Value}"
                    : "Dine-In";
            }
            else if (order.OrderType == OrderType.Delivery)
            {
                tableOrDelivery = !string.IsNullOrWhiteSpace(order.DeliveryCustomerName)
                    ? $"Delivery · {order.DeliveryCustomerName}"
                    : !string.IsNullOrWhiteSpace(order.RiderName) ? $"Delivery · Rider {order.RiderName}" : "Delivery";
            }
            else
            {
                tableOrDelivery = "Take Away";
            }

            // Resolve Customer Name (registered customer order must NEVER say "Guest")
            string customerName;
            Guid? effectiveCustomerId = null;
            if (order.CustomerId.HasValue && customers.TryGetValue(order.CustomerId.Value, out var cust))
            {
                customerName = cust.Name;
                effectiveCustomerId = cust.Id.Value;
            }
            else if (!string.IsNullOrWhiteSpace(order.DeliveryCustomerName))
            {
                customerName = order.DeliveryCustomerName;
            }
            else
            {
                customerName = defaultCustomer?.Name ?? "Walk-in Guest";
                effectiveCustomerId = defaultCustomer?.Id.Value;
            }

            // Payment summary & tender split
            decimal orderPaid = 0m;
            decimal orderOnAccount = 0m;
            var paymentParts = new List<string>();
            foreach (var p in activePayments)
            {
                var methodName = paymentMethods.TryGetValue(p.PaymentMethodId, out var pm) ? pm.Name.Value : "Unknown";
                paymentParts.Add($"{methodName} ({p.Amount:N2})");

                if (onAccountMethodIds.Contains(p.PaymentMethodId))
                {
                    onAccountCreated += p.Amount;
                    orderOnAccount += p.Amount;

                    var cur = paymentMethodTotals.GetValueOrDefault(methodName);
                    paymentMethodTotals[methodName] = (cur.Count + 1, cur.Total + p.Amount, "On Account (Credit)");
                }
                else if (advanceMethodIds.Contains(p.PaymentMethodId))
                {
                    // Existing customer advance applied - does NOT count as new cash
                    periodAdvancesApplied += p.Amount;
                    orderPaid += p.Amount;

                    var cur = paymentMethodTotals.GetValueOrDefault(methodName);
                    paymentMethodTotals[methodName] = (cur.Count + 1, cur.Total + p.Amount, "Advance Settlement (Applied)");
                }
                else
                {
                    orderPaid += p.Amount;
                    totalCollected += p.Amount;

                    string cat = "Order Settlement (Other)";
                    if (string.Equals(methodName, CashMethodName, StringComparison.OrdinalIgnoreCase))
                    {
                        cashCollected += p.Amount;
                        cat = "Order Settlement (Cash)";
                    }
                    else if (methodName.Contains(CardFragment, StringComparison.OrdinalIgnoreCase))
                    {
                        cardCollected += p.Amount;
                        cat = "Order Settlement (Card)";
                    }
                    else
                    {
                        otherCollected += p.Amount;
                    }

                    var cur = paymentMethodTotals.GetValueOrDefault(methodName);
                    paymentMethodTotals[methodName] = (cur.Count + 1, cur.Total + p.Amount, cat);
                }
            }

            if (orderOnAccount == 0m && orderPaid < orderTotal && order.CustomerId.HasValue)
            {
                var unpaidCredit = orderTotal - orderPaid;
                orderOnAccount = unpaidCredit;
                onAccountCreated += unpaidCredit;
                var cur = paymentMethodTotals.GetValueOrDefault("On Account (Credit)");
                paymentMethodTotals["On Account (Credit)"] = (cur.Count + 1, cur.Total + unpaidCredit, "On Account (Credit)");
            }

            // Build line item rows for master-detail
            var detailLines = new List<ExpandedOrderLineRowDto>();
            decimal orderQtyTotal = 0m;
            foreach (var l in activeLines)
            {
                orderQtyTotal += l.Quantity;
                var vId = l.ProductVariantId.Value;
                variants.TryGetValue(vId, out var vDto);
                var itemName = vDto != null && products.TryGetValue(vDto.ProductId, out var prodDto)
                    ? (vDto.Name.Equals("Standard", StringComparison.OrdinalIgnoreCase) || vDto.Name.Equals("Single", StringComparison.OrdinalIgnoreCase)
                        ? prodDto.Name
                        : $"{prodDto.Name} ({vDto.Name})")
                    : (vDto?.Name ?? "Item");
                var itemType = vDto?.ItemType ?? "Prepared";
                var unitCost = costPrices.GetValueOrDefault(vId, 0m);

                detailLines.Add(new ExpandedOrderLineRowDto(
                    itemName,
                    vDto?.Sku ?? "-",
                    l.Quantity,
                    l.UnitPrice,
                    0m,
                    l.TaxRatePercentage,
                    0m,
                    l.LineTotal,
                    itemType));

                // Variant sales aggregation
                var (curQty, curGross, curDisc, curNet, curCost) = variantAggregates.GetValueOrDefault(vId);
                decimal lineCost = string.Equals(itemType, "PurchasedResale", StringComparison.OrdinalIgnoreCase)
                    ? l.Quantity * unitCost
                    : 0m;

                variantAggregates[vId] = (curQty + l.Quantity, curGross + l.LineTotal, curDisc, curNet + l.LineTotal, curCost + lineCost);
            }

            // Customer sales roll-up
            if (effectiveCustomerId.HasValue)
            {
                var cId = effectiveCustomerId.Value;
                var (cCount, cQty, cItemSales, cDisc, cFees, cTax, cBillTotal, cPaid, cOnAccount) = customerSales.GetValueOrDefault(cId);
                customerSales[cId] = (
                    cCount + 1,
                    cQty + orderQtyTotal,
                    cItemSales + orderSubtotal,
                    cDisc + orderDiscount,
                    cFees + orderServiceAndDelivery,
                    cTax + orderExclusiveTax,
                    cBillTotal + orderTotal,
                    cPaid + orderPaid,
                    cOnAccount + orderOnAccount);
            }

            orderRows.Add(new ExpandedOrderRowDto(
                order.Id.Value,
                order.OrderNumber.Value,
                order.DailySalesNumber,
                order.OrderType.ToString(),
                order.OrderSource.ToString(),
                tableOrDelivery,
                customerName,
                activeLines.Count,
                orderSubtotal,
                orderDiscount,
                orderServiceAndDelivery,
                orderTax,
                orderTotal,
                order.Status.ToString(),
                order.CreatedAtUtc,
                paymentParts.Count > 0 ? string.Join(", ", paymentParts) : "(none)",
                orderPaid,
                orderOnAccount,
                Math.Max(0m, orderTotal - orderPaid - orderOnAccount),
                detailLines));
        }

        // Voided orders totals
        decimal voidedOrdersAmount = 0m;
        foreach (var vOrder in voidedOrders)
        {
            var vLines = await orderLineRepository.GetByOrderIdAsync(vOrder.Id, cancellationToken);
            voidedOrdersAmount += vLines.Where(l => !l.IsVoided).Sum(l => l.LineTotal);
        }

        // Customer Ledger analysis for the period
        decimal periodCustomerPaymentsCollected = 0m;
        decimal periodAdvancesReceived = 0m;
        var customerPaymentsByMethod = new Dictionary<string, (int Count, decimal Total)>();

        var fromUtc = businessDateProvider != null
            ? businessDateProvider.GetUtcRangeForBusinessDate(request.FromDate).StartUtc
            : new DateTimeOffset(request.FromDate.ToDateTime(TimeOnly.MinValue), DateTimeOffset.Now.Offset).ToUniversalTime();

        var toUtc = businessDateProvider != null
            ? businessDateProvider.GetUtcRangeForBusinessDate(request.ToDate).EndUtc
            : new DateTimeOffset(request.ToDate.ToDateTime(TimeOnly.MaxValue), DateTimeOffset.Now.Offset).ToUniversalTime();

        var receivableRows = new List<ExpandedReceivableActivityRowDto>();
        var customerReportRows = new List<ExpandedCustomerRowDto>();

        foreach (var customer in customers.Values)
        {
            var customerLedger = await ledgerRepository.GetByCustomerIdAsync(customer.Id, cancellationToken);
            var sortedLedger = customerLedger.OrderBy(e => e.Date).ToList();

            var priorEntries = sortedLedger.Where(e => ToBusinessDate(e.Date) < request.FromDate).ToList();
            var periodEntries = sortedLedger.Where(e => ToBusinessDate(e.Date) >= request.FromDate && ToBusinessDate(e.Date) <= request.ToDate).ToList();

            decimal priorNet = priorEntries.Count > 0
                ? priorEntries.Last().RunningBalance
                : (ToBusinessDate(customer.CreatedAtUtc) < request.FromDate ? customer.OpeningBalance : 0m);

            decimal openReceivable = Math.Max(0m, priorNet);
            decimal openAdvance = Math.Max(0m, -priorNet);

            decimal running = priorNet;
            decimal newOnAccount = 0m;
            decimal collectionsAppliedToAR = 0m;
            decimal advanceAppliedToAR = 0m;
            decimal advanceCreated = 0m;
            decimal advanceUsed = 0m;
            decimal totalCustomerPayments = 0m;

            foreach (var pe in periodEntries)
            {
                if (pe.Debit > 0m)
                {
                    bool isAdvanceSettlement = pe.Description.StartsWith("Customer Advance Settlement", StringComparison.OrdinalIgnoreCase);

                    if (running < 0m)
                    {
                        decimal existingAdvance = -running;
                        decimal absorbed = Math.Min(pe.Debit, existingAdvance);
                        advanceUsed += absorbed;

                        if (!isAdvanceSettlement)
                        {
                            newOnAccount += pe.Debit;
                            advanceAppliedToAR += absorbed;
                        }
                    }
                    else
                    {
                        if (!isAdvanceSettlement)
                        {
                            newOnAccount += pe.Debit;
                        }
                    }
                    running += pe.Debit;
                }

                if (pe.Credit > 0m)
                {
                    totalCustomerPayments += pe.Credit;
                    var pmName = !string.IsNullOrWhiteSpace(pe.PaymentMethod) ? pe.PaymentMethod : "Cash";
                    var cur = customerPaymentsByMethod.GetValueOrDefault(pmName);
                    customerPaymentsByMethod[pmName] = (cur.Count + 1, cur.Total + pe.Credit);
                    periodCustomerPaymentsCollected += pe.Credit;

                    if (running > 0m)
                    {
                        decimal applied = Math.Min(pe.Credit, running);
                        decimal excess = pe.Credit - applied;
                        collectionsAppliedToAR += applied;
                        advanceCreated += excess;
                    }
                    else
                    {
                        advanceCreated += pe.Credit;
                    }
                    running -= pe.Credit;
                }
            }

            decimal closingNet = periodEntries.Count > 0
                ? periodEntries.Last().RunningBalance
                : priorNet;

            decimal closingReceivable = Math.Max(0m, closingNet);
            decimal closingAdvance = Math.Max(0m, -closingNet);

            periodAdvancesReceived += advanceCreated;

            // Only report customer if they have activity or non-zero balance
            if (openReceivable > 0 || newOnAccount > 0 || collectionsAppliedToAR > 0 || closingReceivable > 0 || openAdvance > 0 || advanceCreated > 0 || advanceUsed > 0 || closingAdvance > 0)
            {
                receivableRows.Add(new ExpandedReceivableActivityRowDto(
                    customer.Id.Value,
                    customer.Code.Value,
                    customer.Name,
                    openReceivable,
                    newOnAccount,
                    collectionsAppliedToAR,
                    advanceAppliedToAR,
                    closingReceivable,
                    openAdvance,
                    advanceCreated,
                    advanceUsed,
                    closingAdvance));
            }

            var (ordCount, qtyTotal, cItemSales, cDisc, cFees, cTax, cBillTotal, cPaid, cOnAcc) = customerSales.GetValueOrDefault(customer.Id.Value);
            if (ordCount > 0 || totalCustomerPayments > 0 || closingReceivable > 0 || closingAdvance > 0)
            {
                customerReportRows.Add(new ExpandedCustomerRowDto(
                    customer.Id.Value,
                    customer.Code.Value,
                    customer.Name,
                    customer.MobileNumber,
                    ordCount,
                    qtyTotal,
                    cItemSales,
                    cDisc,
                    cFees,
                    cBillTotal,
                    cPaid,
                    cOnAcc,
                    totalCustomerPayments,
                    closingReceivable,
                    closingAdvance,
                    cTax));
            }
        }

        // Shifts summary in range & cash drawer reconciliation
        var shiftsInRange = await shiftRepository.SearchShiftsAsync(
            fromDateUtc: fromUtc,
            toDateUtc: toUtc,
            cancellationToken: cancellationToken);
        int shiftCount = shiftsInRange.Count;
        decimal totalShiftVariance = shiftsInRange.Sum(s => s.CashVariance);

        var shiftDrawerRows = new List<ShiftDrawerCashSummaryDto>();
        foreach (var s in shiftsInRange.OrderBy(s => s.ShiftNumber))
        {
            var shiftPayments = await paymentRepository.GetByShiftIdAsync(s.Id, cancellationToken);
            decimal shiftCashSales = 0m;
            foreach (var p in shiftPayments.Where(p => !p.IsVoided))
            {
                if (paymentMethods.TryGetValue(p.PaymentMethodId, out var pm) &&
                    string.Equals(pm.Name.Value, "Cash", StringComparison.OrdinalIgnoreCase))
                {
                    shiftCashSales += p.Amount;
                }
            }

            var shiftLedger = await ledgerRepository.GetByShiftIdAsync(s.Id, cancellationToken);
            decimal shiftCashCollections = shiftLedger
                .Where(e => e.Credit > 0 && string.Equals(e.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.Credit);

            decimal shiftCashIn = s.CashMovements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount);
            decimal shiftCashOut = s.CashMovements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount);

            decimal shiftExpected = s.Status == ShiftStatus.Closed
                ? s.ExpectedCash
                : (s.StartingCash + shiftCashIn + shiftCashSales + shiftCashCollections - shiftCashOut);

            decimal? shiftCounted = s.Status == ShiftStatus.Closed ? s.CountedCash : null;
            decimal? shiftVariance = s.Status == ShiftStatus.Closed ? s.CashVariance : null;

            shiftDrawerRows.Add(new ShiftDrawerCashSummaryDto(
                s.Id.Value,
                s.ShiftNumber,
                s.CashierName,
                s.StartingCash,
                shiftCashSales,
                shiftCashCollections,
                shiftCashIn,
                shiftCashOut,
                shiftExpected,
                shiftCounted,
                shiftVariance,
                s.Status.ToString()));
        }

        decimal itemSales = grossSales;
        decimal totalBillSales = grossSales - totalDiscounts + totalDeliveryFees + totalServiceCharges;
        int totalOrders = completedOrders.Count;
        decimal averageOrderValue = totalOrders > 0 ? totalBillSales / totalOrders : 0m;
        decimal closingTotalReceivables = receivableRows.Sum(r => r.ClosingReceivable);

        var kpis = new SalesSummaryKpiDto(
            grossSales,
            totalDiscounts,
            totalServiceCharges,
            totalDeliveryFees,
            totalBillSales,
            totalTax,
            totalCollected,
            cashCollected,
            cardCollected,
            otherCollected,
            onAccountCreated,
            periodCustomerPaymentsCollected,
            closingTotalReceivables,
            periodAdvancesReceived,
            periodAdvancesApplied,
            deliverySales,
            totalOrders,
            averageOrderValue,
            voidedOrders.Count,
            voidedOrdersAmount,
            shiftCount,
            totalShiftVariance,
            itemSales,
            totalBillSales);

        // Build Items rows (Detailed Items tab)
        var itemRows = new List<ExpandedItemRowDto>();
        foreach (var (vId, agg) in variantAggregates)
        {
            variants.TryGetValue(vId, out var variantDto);
            var itemName = variantDto != null && products.TryGetValue(variantDto.ProductId, out var pDto)
                ? (variantDto.Name.Equals("Standard", StringComparison.OrdinalIgnoreCase) || variantDto.Name.Equals("Single", StringComparison.OrdinalIgnoreCase)
                    ? pDto.Name
                    : $"{pDto.Name} ({variantDto.Name})")
                : (variantDto?.Name ?? "Item");
            var itemType = variantDto?.ItemType ?? "Prepared";
            string catName = "General";
            if (variantDto != null && products.TryGetValue(variantDto.ProductId, out var pDto2) && pDto2.CategoryId.HasValue && categories.TryGetValue(pDto2.CategoryId.Value, out var catDto))
            {
                catName = catDto.Name;
            }

            var unitPrice = agg.Quantity > 0 ? agg.GrossSales / agg.Quantity : 0m;
            var costPrice = costPrices.GetValueOrDefault(vId, 0m);

            decimal? itemCost = null;
            decimal? grossProfit = null;
            decimal? marginPct = null;

            if (string.Equals(itemType, "PurchasedResale", StringComparison.OrdinalIgnoreCase))
            {
                itemCost = agg.Quantity * costPrice;
                grossProfit = agg.GrossSales - itemCost.Value;
                marginPct = agg.GrossSales > 0 ? (grossProfit.Value / agg.GrossSales) * 100m : 0m;
            }
            else if (string.Equals(itemType, "Service", StringComparison.OrdinalIgnoreCase))
            {
                itemCost = agg.Quantity * costPrice; // typically 0
                grossProfit = agg.GrossSales - itemCost.Value;
                marginPct = agg.GrossSales > 0 ? (grossProfit.Value / agg.GrossSales) * 100m : 100.0m;
            }
            else // Prepared
            {
                if (costPrice > 0)
                {
                    itemCost = agg.Quantity * costPrice;
                    grossProfit = agg.GrossSales - itemCost.Value;
                    marginPct = agg.GrossSales > 0 ? (grossProfit.Value / agg.GrossSales) * 100m : 0m;
                }
                // If costPrice == 0, keep itemCost, grossProfit, marginPct as null (displays N/A)
            }

            var percentOfTotal = grossSales > 0 ? (agg.GrossSales / grossSales) * 100m : 0m;

            decimal? costPriceDto = costPrice;
            if (string.Equals(itemType, "Prepared", StringComparison.OrdinalIgnoreCase) && costPrice <= 0)
            {
                costPriceDto = null;
            }

            itemRows.Add(new ExpandedItemRowDto(
                vId,
                catName,
                itemName,
                itemType,
                agg.Quantity,
                Math.Round(unitPrice, 2),
                costPriceDto.HasValue ? Math.Round(costPriceDto.Value, 2) : null,
                agg.GrossSales,
                itemCost.HasValue ? Math.Round(itemCost.Value, 2) : null,
                grossProfit.HasValue ? Math.Round(grossProfit.Value, 2) : null,
                marginPct.HasValue ? Math.Round(marginPct.Value, 1) : null,
                Math.Round(percentOfTotal, 1)));
        }

        // Build Payments rows: clearly distinguish Order Settlements, On Account, and Customer Collections
        var paymentRows = new List<ExpandedPaymentRowDto>();
        foreach (var (methodName, pData) in paymentMethodTotals)
        {
            var pct = (totalCollected + onAccountCreated + periodAdvancesApplied) > 0
                ? (pData.Total / (totalCollected + onAccountCreated + periodAdvancesApplied)) * 100m
                : 0m;

            paymentRows.Add(new ExpandedPaymentRowDto(
                methodName,
                pData.Count,
                pData.Total,
                Math.Round(pct, 1),
                pData.Category));
        }

        foreach (var (methodName, cData) in customerPaymentsByMethod)
        {
            var pct = periodCustomerPaymentsCollected > 0 ? (cData.Total / periodCustomerPaymentsCollected) * 100m : 0m;
            paymentRows.Add(new ExpandedPaymentRowDto(
                $"{methodName} (Account Collection)",
                cData.Count,
                cData.Total,
                Math.Round(pct, 1),
                "Customer Collection (A/R)"));
        }

        // Order Types Breakdown (Phase 8)
        var orderTypeRows = new List<ExpandedOrderTypeBreakdownDto>();
        var orderTypes = new[] { OrderType.DineIn, OrderType.TakeAway, OrderType.Delivery };
        foreach (var oType in orderTypes)
        {
            var matchingOrders = completedOrders.Where(o => o.OrderType == oType).ToList();
            var oCount = matchingOrders.Count;
            var oRows = orderRows.Where(r => r.OrderType == oType.ToString()).ToList();
            var oQtySold = oRows.Sum(r => r.Lines?.Sum(l => l.Quantity) ?? 0m);
            var oGross = oRows.Sum(r => r.Subtotal);
            var oDisc = oRows.Sum(r => r.Discount);
            var oFees = matchingOrders.Sum(o => o.DeliveryFee);
            var oNet = oGross - oDisc + oFees;
            var aov = oCount > 0 ? oNet / oCount : 0m;
            var pct = totalBillSales > 0 ? (oNet / totalBillSales) * 100m : 0m;

            orderTypeRows.Add(new ExpandedOrderTypeBreakdownDto(
                oType.ToString(),
                oCount,
                oQtySold,
                oGross,
                oDisc,
                oFees,
                oNet,
                Math.Round(aov, 2),
                Math.Round(pct, 1)));
        }

        // Item Classification & Profitability Breakdown (Phase 9)
        var itemClassRows = new List<ExpandedItemClassificationBreakdownDto>();
        var requiredTypes = new[] { "Prepared", "PurchasedResale", "Service" };

        foreach (var t in requiredTypes)
        {
            var matchingItems = itemRows.Where(i => string.Equals(i.ItemType, t, StringComparison.OrdinalIgnoreCase)).ToList();
            decimal q = matchingItems.Sum(i => i.QuantitySold);
            decimal s = matchingItems.Sum(i => i.TotalSales);

            if (string.Equals(t, "Prepared", StringComparison.OrdinalIgnoreCase))
            {
                var configured = matchingItems.Where(i => i.EstimatedCost.HasValue).ToList();
                if (configured.Count > 0 && configured.Count == matchingItems.Count)
                {
                    decimal c = configured.Sum(i => i.EstimatedCost!.Value);
                    decimal p = s - c;
                    decimal m = s > 0 ? (p / s) * 100m : 0m;
                    itemClassRows.Add(new ExpandedItemClassificationBreakdownDto(
                        "Prepared",
                        q,
                        s,
                        c,
                        p,
                        Math.Round(m, 1),
                        c.ToString("N2")));
                }
                else
                {
                    // Food cost is unconfigured - display N/A (Phase 9)
                    itemClassRows.Add(new ExpandedItemClassificationBreakdownDto(
                        "Prepared",
                        q,
                        s,
                        null,
                        null,
                        null,
                        "N/A"));
                }
            }
            else if (string.Equals(t, "PurchasedResale", StringComparison.OrdinalIgnoreCase))
            {
                decimal c = matchingItems.Sum(i => i.EstimatedCost ?? 0m);
                decimal p = s - c;
                decimal m = s > 0 ? (p / s) * 100m : 0m;
                itemClassRows.Add(new ExpandedItemClassificationBreakdownDto(
                    "PurchasedResale",
                    q,
                    s,
                    c,
                    p,
                    Math.Round(m, 1),
                    c.ToString("N2")));
            }
            else // Service
            {
                decimal c = matchingItems.Sum(i => i.EstimatedCost ?? 0m);
                decimal p = s - c;
                decimal m = s > 0 ? (p / s) * 100m : 100.0m;
                itemClassRows.Add(new ExpandedItemClassificationBreakdownDto(
                    "Service",
                    q,
                    s,
                    c,
                    p,
                    Math.Round(m, 1),
                    c.ToString("N2")));
            }
        }

        return new ExpandedSalesSummaryDto(
            request.WarehouseId,
            request.FromDate,
            request.ToDate,
            kpis,
            orderRows,
            [.. itemRows.OrderByDescending(i => i.QuantitySold)],
            [.. customerReportRows.OrderByDescending(c => c.NetSales)],
            [.. paymentRows.OrderBy(p => p.Category).ThenByDescending(p => p.TotalCollected)],
            [.. receivableRows.OrderByDescending(r => r.ClosingReceivable)],
            orderTypeRows,
            itemClassRows,
            shiftDrawerRows);
    }
}
