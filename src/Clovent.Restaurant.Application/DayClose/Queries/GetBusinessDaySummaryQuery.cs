using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Restaurant.Application.DayClose.Dtos;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.DayClose;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.DayClose.Queries;

/// <summary>Query to retrieve the daily close summary, reconciliation metrics, and open shift prerequisites for a business date.</summary>
public sealed record GetBusinessDaySummaryQuery(
    Guid BranchId,
    DateOnly BusinessDate,
    Guid? ShiftId = null) : IRequest<BusinessDaySummaryDto>;

/// <summary>Handles <see cref="GetBusinessDaySummaryQuery"/>.</summary>
public sealed class GetBusinessDaySummaryQueryHandler(
    IBusinessDayCloseRepository dayCloseRepository,
    IShiftRepository shiftRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IBusinessDateProvider businessDateProvider,
    IOrderRepository? orderRepository = null,
    IOrderLineRepository? orderLineRepository = null,
    IRefundRepository? refundRepository = null) : IRequestHandler<GetBusinessDaySummaryQuery, BusinessDaySummaryDto>
{
    /// <inheritdoc/>
    public async Task<BusinessDaySummaryDto> Handle(GetBusinessDaySummaryQuery request, CancellationToken cancellationToken)
    {
        var branchId = new BranchId(request.BranchId);

        // 1. Check if already closed
        var existingClose = await dayCloseRepository.GetByBranchAndDateAsync(branchId, request.BusinessDate, cancellationToken);
        var existingCloseDto = existingClose != null ? BusinessDayCloseDto.FromDomain(existingClose) : null;

        // 2. Resolve UTC window for this operating business date
        var (startUtc, endUtc) = businessDateProvider.GetUtcRangeForBusinessDate(request.BusinessDate);

        // 3. Retrieve shifts for this branch covering this operating period
        var shifts = await shiftRepository.SearchShiftsAsync(
            fromDateUtc: startUtc,
            toDateUtc: endUtc,
            cancellationToken: cancellationToken);

        var branchShifts = shifts.Where(s => s.BranchId == branchId).ToList();

        // Optional cashier/shift filtering
        if (request.ShiftId.HasValue)
        {
            branchShifts = branchShifts.Where(s => s.Id.Value == request.ShiftId.Value).ToList();
        }

        var openShiftsList = branchShifts.Where(s => s.Status == ShiftStatus.Open).ToList();
        var closedShiftsList = branchShifts.Where(s => s.Status == ShiftStatus.Closed).ToList();

        var shiftRefundsByShiftId = new Dictionary<Guid, IReadOnlyList<Refund>>();
        if (refundRepository != null)
        {
            foreach (var s in branchShifts)
            {
                shiftRefundsByShiftId[s.Id.Value] = await refundRepository.GetByShiftIdAsync(s.Id, cancellationToken);
            }
        }

        ShiftDayCloseItemDto MapShiftItem(Shift s)
        {
            var sRefunds = shiftRefundsByShiftId.GetValueOrDefault(s.Id.Value) ?? [];
            var sCashRefunds = sRefunds
                .Where(r => r.SettlementMethod == RefundSettlementMethod.CashPayout)
                .Sum(r => r.GrandTotalRefunded);
            var sCreditRefunds = sRefunds
                .Where(r => r.SettlementMethod == RefundSettlementMethod.CustomerAccountCredit)
                .Sum(r => r.GrandTotalRefunded);

            return new ShiftDayCloseItemDto(
                s.Id.Value,
                s.ShiftNumber,
                s.CashierName,
                s.TerminalId.Value,
                s.OpenedAtUtc,
                s.ClosedAtUtc,
                s.Status.ToString(),
                s.StartingCash,
                s.ExpectedCash,
                s.CountedCash,
                s.CashVariance,
                s.VarianceReason,
                MoneyRoundingPolicy.RoundMoney(sCashRefunds),
                MoneyRoundingPolicy.RoundMoney(sCreditRefunds));
        }

        var openShiftDtos = openShiftsList.Select(MapShiftItem).ToList();
        var closedShiftDtos = closedShiftsList.Select(MapShiftItem).ToList();

        // 4. Payment classification & reconciliation
        var allPaymentMethods = await paymentMethodRepository.GetAllAsync(cancellationToken);
        var methodsById = allPaymentMethods.ToDictionary(m => m.Id);

        decimal totalCashSales = 0m;
        decimal totalCardSales = 0m;
        decimal totalBankTransferSales = 0m;
        decimal totalDigitalSales = 0m;
        decimal totalOtherSales = 0m;
        decimal outstandingReceivables = 0m;

        decimal totalCashIn = 0m;
        decimal totalCashOut = 0m;
        decimal totalVariance = 0m;
        decimal openingFloat = branchShifts.Sum(s => s.StartingCash);
        decimal actualClosingCash = closedShiftsList.Sum(s => s.CountedCash);

        var distinctOrderIds = new HashSet<Guid>();
        var allActivePayments = new List<Payment>();
        var tenderMap = new Dictionary<string, (int Count, decimal Amount)>(StringComparer.OrdinalIgnoreCase);
        var cashierSalesMap = new Dictionary<string, (int OrderCount, decimal Sales)>(StringComparer.OrdinalIgnoreCase);

        foreach (var shift in branchShifts)
        {
            totalCashIn += shift.CashMovements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount);
            totalCashOut += shift.CashMovements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount);

            if (shift.Status == ShiftStatus.Closed)
            {
                totalVariance += shift.CashVariance;
            }

            var payments = await paymentRepository.GetByShiftIdAsync(shift.Id, cancellationToken);
            decimal shiftSales = 0m;
            var shiftOrderIds = new HashSet<Guid>();

            foreach (var p in payments.Where(p => !p.IsVoided))
            {
                allActivePayments.Add(p);
                distinctOrderIds.Add(p.OrderId.Value);
                shiftOrderIds.Add(p.OrderId.Value);
                shiftSales += p.Amount;

                var methodName = methodsById.TryGetValue(p.PaymentMethodId, out var method)
                    ? method.Name.Value
                    : "Other";

                if (PaymentMethodClassifier.IsCash(methodName))
                {
                    totalCashSales += p.Amount;
                }
                else if (PaymentMethodClassifier.IsCard(methodName))
                {
                    totalCardSales += p.Amount;
                }
                else if (PaymentMethodClassifier.IsBankTransfer(methodName))
                {
                    totalBankTransferSales += p.Amount;
                }
                else if (PaymentMethodClassifier.IsDigitalPayment(methodName))
                {
                    totalDigitalSales += p.Amount;
                }
                else if (PaymentMethodClassifier.IsOnAccountCredit(methodName))
                {
                    outstandingReceivables += p.Amount;
                    totalOtherSales += p.Amount;
                }
                else
                {
                    totalOtherSales += p.Amount;
                }

                if (tenderMap.TryGetValue(methodName, out var currTender))
                {
                    tenderMap[methodName] = (currTender.Count + 1, currTender.Amount + p.Amount);
                }
                else
                {
                    tenderMap[methodName] = (1, p.Amount);
                }
            }

            if (cashierSalesMap.TryGetValue(shift.CashierName, out var currCashier))
            {
                cashierSalesMap[shift.CashierName] = (currCashier.OrderCount + shiftOrderIds.Count, currCashier.Sales + shiftSales);
            }
            else
            {
                cashierSalesMap[shift.CashierName] = (shiftOrderIds.Count, shiftSales);
            }
        }

        // Split Payments Analysis
        var paymentsByOrder = allActivePayments
            .GroupBy(p => p.OrderId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        int splitPaymentsCount = paymentsByOrder.Values.Count(list => list.Count > 1);
        decimal splitPaymentsTotal = paymentsByOrder.Values
            .Where(list => list.Count > 1)
            .Sum(list => list.Sum(p => p.Amount));

        decimal totalSales = totalCashSales + totalCardSales + totalBankTransferSales + totalDigitalSales + totalOtherSales;
        decimal totalCollections = totalSales;

        // 5. Orders, Taxes, Discounts & Operational Breakdown
        decimal totalGrossSales = 0m;
        decimal totalItemDiscounts = 0m;
        decimal totalOrderDiscounts = 0m;
        decimal totalTax = 0m;
        decimal totalTaxableSales = 0m;
        decimal totalTaxExemptSales = 0m;
        int completedOrdersCount = 0;
        int cancelledOrdersCount = 0;

        var taxBreakdownMap = new Dictionary<(string Authority, string TaxCode, decimal Rate), (decimal TaxableBase, decimal TaxAmount)>();
        var orderTypeSalesMap = new Dictionary<string, (int OrderCount, decimal Sales)>(StringComparer.OrdinalIgnoreCase);

        if (orderRepository != null)
        {
            foreach (var orderId in distinctOrderIds)
            {
                var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken);
                if (order is null) continue;

                if (order.Status == OrderStatus.Completed)
                {
                    completedOrdersCount++;
                }
                else if (order.Status == OrderStatus.Cancelled)
                {
                    cancelledOrdersCount++;
                }

                var orderTypeKey = order.OrderType.ToString();
                var orderPaidAmount = paymentsByOrder.TryGetValue(order.Id.Value, out var orderPays)
                    ? orderPays.Sum(p => p.Amount)
                    : 0m;

                if (orderTypeSalesMap.TryGetValue(orderTypeKey, out var currOt))
                {
                    orderTypeSalesMap[orderTypeKey] = (currOt.OrderCount + 1, currOt.Sales + orderPaidAmount);
                }
                else
                {
                    orderTypeSalesMap[orderTypeKey] = (1, orderPaidAmount);
                }

                // Financial details from ReceiptSnapshot if available
                if (!string.IsNullOrEmpty(order.ReceiptSnapshotJson))
                {
                    try
                    {
                        var snapshot = JsonSerializer.Deserialize<ReceiptSnapshot>(order.ReceiptSnapshotJson);
                        if (snapshot != null)
                        {
                            totalGrossSales += snapshot.Subtotal;
                            totalTax += snapshot.TaxTotal;
                            totalOrderDiscounts += snapshot.DiscountTotal;
                            totalTaxableSales += snapshot.TotalTaxableBase;
                            totalTaxExemptSales += Math.Max(0m, (snapshot.Subtotal - snapshot.DiscountTotal) - snapshot.TotalTaxableBase);

                            if (snapshot.TaxBreakdown != null)
                            {
                                foreach (var ts in snapshot.TaxBreakdown)
                                {
                                    var key = (ts.Authority, ts.TaxCode, ts.RatePercentage);
                                    if (taxBreakdownMap.TryGetValue(key, out var exTax))
                                    {
                                        taxBreakdownMap[key] = (exTax.TaxableBase + ts.TaxableBase, exTax.TaxAmount + ts.TaxAmount);
                                    }
                                    else
                                    {
                                        taxBreakdownMap[key] = (ts.TaxableBase, ts.TaxAmount);
                                    }
                                }
                            }
                            continue;
                        }
                    }
                    catch
                    {
                        // Fallback to order lines
                    }
                }

                // Fallback to OrderLine analysis
                if (orderLineRepository != null)
                {
                    var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
                    var activeLines = lines.Where(l => !l.IsVoided).ToList();

                    var lineGross = activeLines.Sum(l => MoneyRoundingPolicy.RoundMoney(l.Quantity * l.UnitPrice));
                    totalGrossSales += lineGross;

                    var orderDisc = activeLines.Sum(l => l.AllocatedDiscount);
                    totalOrderDiscounts += orderDisc;

                    var orderTax = activeLines.Sum(l => l.TaxAmount ?? 0m);
                    totalTax += orderTax;

                    foreach (var l in activeLines)
                    {
                        var lineNet = l.Quantity * l.UnitPrice - l.AllocatedDiscount;
                        if (l.TaxRatePercentage > 0m && string.Equals(l.TaxClassification, "Taxable", StringComparison.OrdinalIgnoreCase))
                        {
                            var baseAmount = l.TaxableBase ?? lineNet;
                            totalTaxableSales += baseAmount;
                            var key = (l.TaxAuthority ?? "PRA", l.TaxCode ?? $"TAX-{l.TaxRatePercentage:0.##}", l.TaxRatePercentage);
                            if (taxBreakdownMap.TryGetValue(key, out var exTax))
                            {
                                taxBreakdownMap[key] = (exTax.TaxableBase + baseAmount, exTax.TaxAmount + (l.TaxAmount ?? 0m));
                            }
                            else
                            {
                                taxBreakdownMap[key] = (baseAmount, l.TaxAmount ?? 0m);
                            }
                        }
                        else
                        {
                            totalTaxExemptSales += lineNet;
                        }
                    }
                }
            }
        }

        // If gross sales was not computable via lines or snapshots, derive from sales and discounts
        decimal totalDiscounts = totalItemDiscounts + totalOrderDiscounts;
        if (totalGrossSales == 0m && totalSales > 0m)
        {
            totalGrossSales = totalSales + totalDiscounts - totalTax;
        }

        decimal netSales = Math.Max(0m, totalGrossSales - totalDiscounts);
        decimal averageOrderValue = completedOrdersCount > 0
            ? MoneyRoundingPolicy.RoundMoney(netSales / completedOrdersCount)
            : (distinctOrderIds.Count > 0 ? MoneyRoundingPolicy.RoundMoney(netSales / distinctOrderIds.Count) : 0m);

        // 6. Refunds & Reversals
        decimal totalRefunds = 0m;
        decimal cashRefunds = 0m;
        decimal customerAccountCreditRefunds = 0m;
        decimal otherRefunds = 0m;
        int refundedOrdersCount = 0;

        if (refundRepository != null)
        {
            var dayRefunds = await refundRepository.GetByDateRangeAsync(startUtc, endUtc, cancellationToken);
            var branchRefundsById = dayRefunds
                .Where(r => r.BranchId == branchId)
                .ToDictionary(r => r.Id);

            foreach (var sRefunds in shiftRefundsByShiftId.Values)
            {
                foreach (var sr in sRefunds)
                {
                    branchRefundsById.TryAdd(sr.Id, sr);
                }
            }

            var branchRefunds = branchRefundsById.Values.ToList();
            totalRefunds = branchRefunds.Sum(r => r.GrandTotalRefunded);
            cashRefunds = branchRefunds
                .Where(r => r.SettlementMethod == RefundSettlementMethod.CashPayout)
                .Sum(r => r.GrandTotalRefunded);
            customerAccountCreditRefunds = branchRefunds
                .Where(r => r.SettlementMethod == RefundSettlementMethod.CustomerAccountCredit)
                .Sum(r => r.GrandTotalRefunded);
            otherRefunds = branchRefunds
                .Where(r => r.SettlementMethod != RefundSettlementMethod.CashPayout &&
                            r.SettlementMethod != RefundSettlementMethod.CustomerAccountCredit)
                .Sum(r => r.GrandTotalRefunded);

            refundedOrdersCount = branchRefunds.Select(r => r.OrderId).Distinct().Count();
        }

        // 7. Cash Drawer Expected vs Actual
        decimal expectedClosingCash = MoneyRoundingPolicy.RoundMoney(openingFloat + totalCashSales + totalCashIn - totalCashOut - cashRefunds);

        // Map breakdowns
        var tenderBreakdown = tenderMap
            .Select(t => new DayCloseTenderItemDto(t.Key, t.Value.Count, MoneyRoundingPolicy.RoundMoney(t.Value.Amount)))
            .OrderByDescending(t => t.TotalAmount)
            .ToList();

        var taxBreakdown = taxBreakdownMap
            .Select(t => new DayCloseTaxItemDto(t.Key.Authority, t.Key.TaxCode, t.Key.Rate, MoneyRoundingPolicy.RoundMoney(t.Value.TaxableBase), MoneyRoundingPolicy.RoundMoney(t.Value.TaxAmount)))
            .OrderBy(t => t.TaxCode)
            .ToList();

        var cashierBreakdown = cashierSalesMap
            .Select(c => new DayCloseCashierSalesDto(c.Key, c.Value.OrderCount, MoneyRoundingPolicy.RoundMoney(c.Value.Sales)))
            .OrderByDescending(c => c.TotalSales)
            .ToList();

        var orderTypeBreakdown = orderTypeSalesMap
            .Select(o => new DayCloseOrderTypeSalesDto(o.Key, o.Value.OrderCount, MoneyRoundingPolicy.RoundMoney(o.Value.Sales)))
            .OrderByDescending(o => o.TotalSales)
            .ToList();

        bool isAlreadyClosed = existingClose != null;
        bool canClose = !isAlreadyClosed && openShiftsList.Count == 0;

        string? blockingReason = null;
        if (isAlreadyClosed)
        {
            blockingReason = $"Business day {request.BusinessDate:yyyy-MM-dd} for this location has already been closed.";
        }
        else if (openShiftsList.Count > 0)
        {
            blockingReason = $"{openShiftsList.Count} shift(s) are still open. All cashier shifts must be closed before closing the business day.";
        }

        return new BusinessDaySummaryDto(
            request.BranchId,
            request.BusinessDate,
            isAlreadyClosed,
            existingCloseDto,
            openShiftDtos,
            closedShiftDtos,
            branchShifts.Count,
            distinctOrderIds.Count,
            MoneyRoundingPolicy.RoundMoney(totalSales),
            MoneyRoundingPolicy.RoundMoney(totalCashSales),
            MoneyRoundingPolicy.RoundMoney(totalCardSales),
            MoneyRoundingPolicy.RoundMoney(totalOtherSales),
            MoneyRoundingPolicy.RoundMoney(totalRefunds),
            MoneyRoundingPolicy.RoundMoney(totalDiscounts),
            MoneyRoundingPolicy.RoundMoney(totalTax),
            MoneyRoundingPolicy.RoundMoney(totalCashIn),
            MoneyRoundingPolicy.RoundMoney(totalCashOut),
            MoneyRoundingPolicy.RoundMoney(totalVariance),
            canClose,
            blockingReason,
            MoneyRoundingPolicy.RoundMoney(cashRefunds),
            MoneyRoundingPolicy.RoundMoney(customerAccountCreditRefunds),
            MoneyRoundingPolicy.RoundMoney(otherRefunds),
            // Extended Sales Breakdown
            GrossSales: MoneyRoundingPolicy.RoundMoney(totalGrossSales),
            ItemDiscounts: MoneyRoundingPolicy.RoundMoney(totalItemDiscounts),
            OrderDiscounts: MoneyRoundingPolicy.RoundMoney(totalOrderDiscounts),
            NetSales: MoneyRoundingPolicy.RoundMoney(netSales),
            CancelledOrdersCount: cancelledOrdersCount,
            // Extended Tax Breakdown
            TaxableSales: MoneyRoundingPolicy.RoundMoney(totalTaxableSales),
            TaxExemptSales: MoneyRoundingPolicy.RoundMoney(totalTaxExemptSales),
            TaxBreakdown: taxBreakdown,
            // Extended Payments Breakdown
            BankTransferSales: MoneyRoundingPolicy.RoundMoney(totalBankTransferSales),
            DigitalSales: MoneyRoundingPolicy.RoundMoney(totalDigitalSales),
            SplitPaymentsCount: splitPaymentsCount,
            SplitPaymentsTotal: MoneyRoundingPolicy.RoundMoney(splitPaymentsTotal),
            TotalCollections: MoneyRoundingPolicy.RoundMoney(totalCollections),
            OutstandingReceivables: MoneyRoundingPolicy.RoundMoney(outstandingReceivables),
            TenderBreakdown: tenderBreakdown,
            // Extended Cash Drawer Reconciliation
            OpeningFloat: MoneyRoundingPolicy.RoundMoney(openingFloat),
            ExpectedClosingCash: expectedClosingCash,
            ActualClosingCash: MoneyRoundingPolicy.RoundMoney(actualClosingCash),
            // Extended Operations Breakdown
            CompletedOrdersCount: completedOrdersCount,
            RefundedOrdersCount: refundedOrdersCount,
            AverageOrderValue: averageOrderValue,
            CashierBreakdown: cashierBreakdown,
            OrderTypeBreakdown: orderTypeBreakdown);
    }
}
