using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.AuditAlerts;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.CashierAudits.Services;

/// <summary>
/// Core automated audit analyzer service that scans shifts, orders, activity logs,
/// and tenders for high-risk cashier anomalies, fraud patterns, and shrinkage vectors.
/// </summary>
public sealed class CashierAuditAnalyzerService(
    IShiftRepository shiftRepository,
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IDiscountRepository discountRepository,
    IActivityLogEntryRepository activityLogRepository,
    ICashierAuditAlertRepository alertRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<CashierAuditAnalyzerService> logger) : ICashierAuditAnalyzerService
{
    /// <inheritdoc/>
    public async Task<CashierAuditAnalysisResultDto> RunAnalysisAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        ShiftId? specificShiftId = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTo = toUtc ?? DateTimeOffset.UtcNow;
        var effectiveFrom = fromUtc ?? effectiveTo.AddDays(-7);

        logger.LogInformation("Beginning automated cashier audit analysis from {From} to {To} (Shift: {ShiftId})...",
            effectiveFrom, effectiveTo, specificShiftId);

        // 1. Load relevant shifts
        IReadOnlyList<Shift> shifts;
        if (specificShiftId.HasValue)
        {
            var single = await shiftRepository.GetByIdAsync(specificShiftId.Value, cancellationToken);
            shifts = single != null ? [single] : [];
        }
        else
        {
            shifts = await shiftRepository.SearchShiftsAsync(
                fromDateUtc: effectiveFrom,
                toDateUtc: effectiveTo,
                cancellationToken: cancellationToken);
        }

        // 2. Load payment methods to identify Cash tender
        var paymentMethods = await paymentMethodRepository.GetAllAsync(cancellationToken);
        var cashMethodIds = paymentMethods
            .Where(m => m.Name.Value.Contains("Cash", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToHashSet();

        // 3. Load existing alerts for deduplication
        var existingAlerts = await alertRepository.SearchAsync(
            fromUtc: effectiveFrom.AddDays(-1),
            toUtc: effectiveTo.AddDays(1),
            cancellationToken: cancellationToken);

        var existingFingerprints = existingAlerts
            .Select(a => BuildFingerprint(a.AnomalyType, a.CashierName, a.ShiftId?.Value, a.OrderId?.Value, a.Description))
            .ToHashSet();

        var newAlerts = new List<CashierAuditAlert>();
        var totalOrdersScanned = 0;

        // 4. Load activity logs in scope for Manager Overrides and Drawer Openings
        var allLogs = await activityLogRepository.ListRecentAsync(1000, cancellationToken);
        var activityLogs = allLogs.Where(l => l.OccurredAtUtc >= effectiveFrom && l.OccurredAtUtc <= effectiveTo).ToList();

        // Analyze per shift
        foreach (var shift in shifts)
        {
            var shiftOrders = await LoadOrdersForShiftAsync(shift, cancellationToken);
            totalOrdersScanned += shiftOrders.Count;

            // Rule 1: Excessive Voids After Cash Tender
            await EvaluateExcessiveVoidsAsync(
                shift, shiftOrders, cashMethodIds, existingFingerprints, newAlerts, cancellationToken);

            // Rule 2: Frequent Manager Overrides
            EvaluateFrequentManagerOverrides(
                shift, activityLogs, existingFingerprints, newAlerts);

            // Rule 3: Unlinked Cash Drawer Openings
            EvaluateUnlinkedDrawerOpenings(
                shift, shiftOrders, activityLogs, existingFingerprints, newAlerts);

            // Rule 4: Unusual Discount Clusters
            await EvaluateUnusualDiscountClustersAsync(
                shift, shiftOrders, existingFingerprints, newAlerts, cancellationToken);
        }

        // If no shifts but orders exist in range (e.g. historical unassigned orders), analyze orders directly
        if (shifts.Count == 0)
        {
            var allOrders = (await orderRepository.GetAllAsync(cancellationToken))
                .Where(o => o.CreatedAtUtc >= effectiveFrom && o.CreatedAtUtc <= effectiveTo)
                .ToList();
            totalOrdersScanned += allOrders.Count;

            await EvaluateExcessiveVoidsAcrossOrdersAsync(
                allOrders, cashMethodIds, existingFingerprints, newAlerts, cancellationToken);
        }

        // 5. Persist newly identified alerts and enqueue outbox notifications
        foreach (var alert in newAlerts)
        {
            await alertRepository.AddAsync(alert, cancellationToken);

            var outboxPayload = new CashierAuditAlertOutboxPayload(
                alert.Id.Value,
                alert.CashierName,
                alert.AnomalyType.ToString(),
                alert.Severity.ToString(),
                alert.RiskScore,
                alert.Description,
                alert.DetectedAtUtc,
                alert.SuspectDetailsJson,
                alert.ShiftId?.Value,
                alert.OrderId?.Value);

            var outboxMessage = OutboxMessage.Create(
                OutboxMessageType.CashierAuditAlert,
                "CashierAuditAlert",
                alert.Id.Value.ToString(),
                Guid.NewGuid().ToString(),
                JsonSerializer.Serialize(outboxPayload),
                idempotencyKey: $"AuditAlert-{alert.Id.Value}");

            await outboxRepository.AddAsync(outboxMessage, cancellationToken);
        }

        if (newAlerts.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Automated cashier audit analysis completed: {Count} new alerts generated and outboxed.", newAlerts.Count);
        }

        var allReturnedAlerts = existingAlerts
            .Concat(newAlerts)
            .Select(CashierAuditAlertDto.FromDomain)
            .OrderByDescending(a => a.DetectedAtUtc)
            .ToList();

        return new CashierAuditAnalysisResultDto(
            shifts.Count,
            totalOrdersScanned,
            newAlerts.Count,
            existingAlerts.Count,
            allReturnedAlerts);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CashierRiskScoreDto>> ComputeCashierRiskScoresAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTo = toUtc ?? DateTimeOffset.UtcNow;
        var effectiveFrom = fromUtc ?? effectiveTo.AddDays(-30);

        var shifts = await shiftRepository.SearchShiftsAsync(
            fromDateUtc: effectiveFrom,
            toDateUtc: effectiveTo,
            cancellationToken: cancellationToken);

        var alerts = await alertRepository.SearchAsync(
            fromUtc: effectiveFrom,
            toUtc: effectiveTo,
            cancellationToken: cancellationToken);

        // Group by Cashier Name
        var cashierNames = shifts.Select(s => s.CashierName)
            .Concat(alerts.Select(a => a.CashierName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<CashierRiskScoreDto>();

        foreach (var cashier in cashierNames)
        {
            var cashierShifts = shifts.Where(s => string.Equals(s.CashierName, cashier, StringComparison.OrdinalIgnoreCase)).ToList();
            var cashierAlerts = alerts.Where(a => string.Equals(a.CashierName, cashier, StringComparison.OrdinalIgnoreCase)).ToList();

            var cashierId = cashierShifts.FirstOrDefault()?.CashierId.Value;

            var activeAlerts = cashierAlerts.Where(a => a.Status == AuditAlertStatus.Active || a.Status == AuditAlertStatus.Investigating).ToList();
            var voidAlerts = cashierAlerts.Count(a => a.AnomalyType == CashierAnomalyType.ExcessiveVoidsAfterCashTender);
            var overrideAlerts = cashierAlerts.Count(a => a.AnomalyType == CashierAnomalyType.FrequentManagerOverrides);
            var drawerAlerts = cashierAlerts.Count(a => a.AnomalyType == CashierAnomalyType.UnlinkedCashDrawerOpening);
            var discountAlerts = cashierAlerts.Count(a => a.AnomalyType == CashierAnomalyType.UnusualDiscountCluster);

            // Compute composite risk score (0 - 100)
            decimal overallScore = 0m;
            if (activeAlerts.Count > 0)
            {
                var maxScore = activeAlerts.Max(a => a.RiskScore);
                var additionalRisk = (activeAlerts.Count - 1) * 6.0m;
                overallScore = Math.Min(100.0m, maxScore + additionalRisk);
            }
            else if (cashierAlerts.Count > 0)
            {
                // Has historical reviewed alerts: residual lower risk
                overallScore = Math.Min(30.0m, cashierAlerts.Max(a => a.RiskScore) * 0.3m);
            }

            var riskLevel = overallScore switch
            {
                >= 75.0m => "Critical",
                >= 50.0m => "High",
                >= 25.0m => "Medium",
                _ => "Low"
            };

            // Calculate sparkline trend over the past 7 daily buckets
            var trend = new List<decimal>();
            for (int i = 6; i >= 0; i--)
            {
                var bucketStart = effectiveTo.Date.AddDays(-i);
                var bucketEnd = bucketStart.AddDays(1);
                var dayAlerts = cashierAlerts.Where(a => a.DetectedAtUtc >= bucketStart && a.DetectedAtUtc < bucketEnd).ToList();
                var dayScore = dayAlerts.Count > 0 ? dayAlerts.Max(a => a.RiskScore) : 0m;
                trend.Add(dayScore);
            }

            // Estimate total void amounts and discount amounts from suspect json
            decimal totalVoidAmount = 0m;
            decimal totalDiscountAmount = 0m;
            foreach (var a in cashierAlerts)
            {
                if (a.SuspectDetailsJson is { Length: > 0 } json)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("VoidAmount", out var vAmount))
                            totalVoidAmount += vAmount.GetDecimal();
                        if (doc.RootElement.TryGetProperty("DiscountAmount", out var dAmount))
                            totalDiscountAmount += dAmount.GetDecimal();
                    }
                    catch
                    {
                        // Ignore parse exceptions for free-form metadata
                    }
                }
            }

            var totalSales = cashierShifts.Sum(s => s.StartingCash + s.CountedCash); // Representative activity
            var ordersProcessedEstimate = cashierShifts.Count * 25; // Base heuristic if order links not strictly partitioned

            results.Add(new CashierRiskScoreDto(
                cashierId,
                cashier,
                Math.Round(overallScore, 1),
                riskLevel,
                cashierShifts.Count,
                ordersProcessedEstimate,
                totalSales,
                activeAlerts.Count,
                voidAlerts,
                overrideAlerts,
                drawerAlerts,
                discountAlerts,
                totalVoidAmount,
                totalDiscountAmount,
                trend));
        }

        return results.OrderByDescending(r => r.OverallRiskScore).ToList();
    }

    private async Task<IReadOnlyList<Order>> LoadOrdersForShiftAsync(Shift shift, CancellationToken cancellationToken)
    {
        var shiftClose = shift.ClosedAtUtc ?? DateTimeOffset.UtcNow;
        var orders = await orderRepository.GetAllAsync(cancellationToken);
        return orders
            .Where(o => o.WarehouseId == shift.WarehouseId &&
                        o.CreatedAtUtc >= shift.OpenedAtUtc &&
                        o.CreatedAtUtc <= shiftClose)
            .ToList();
    }

    private async Task EvaluateExcessiveVoidsAsync(
        Shift shift,
        IReadOnlyList<Order> shiftOrders,
        HashSet<PaymentMethodId> cashMethodIds,
        HashSet<string> existingFingerprints,
        List<CashierAuditAlert> newAlerts,
        CancellationToken cancellationToken)
    {
        int totalLineCount = 0;
        int totalVoidedLines = 0;
        decimal totalVoidedAmount = 0m;

        foreach (var order in shiftOrders)
        {
            var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var hasCashTender = payments.Any(p => cashMethodIds.Contains(p.PaymentMethodId) && !p.IsVoided);

            var lines = (await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken)).ToList();
            totalLineCount += lines.Count;

            var voidedLines = lines.Where(l => l.IsVoided).ToList();
            var voidAmountForOrder = voidedLines.Sum(l => l.Quantity * l.UnitPrice);
            totalVoidedLines += voidedLines.Count;
            totalVoidedAmount += voidAmountForOrder;

            // Scenario A: Order completely voided after cash tender was recorded
            if (hasCashTender && order.Status == OrderStatus.Voided)
            {
                var fp = BuildFingerprint(CashierAnomalyType.ExcessiveVoidsAfterCashTender, shift.CashierName, shift.Id.Value, order.Id.Value, "OrderVoidAfterCash");
                if (!existingFingerprints.Contains(fp))
                {
                    existingFingerprints.Add(fp);
                    var details = JsonSerializer.Serialize(new
                    {
                        OrderNumber = order.OrderNumber.Value,
                        VoidAmount = voidAmountForOrder,
                        CashTendered = payments.Where(p => cashMethodIds.Contains(p.PaymentMethodId)).Sum(p => p.Amount),
                        VoidedAtUtc = order.UpdatedAtUtc
                    });

                    newAlerts.Add(CashierAuditAlert.Create(
                        shift.Id,
                        order.Id,
                        shift.CashierId,
                        shift.CashierName,
                        CashierAnomalyType.ExcessiveVoidsAfterCashTender,
                        AuditAlertSeverity.Critical,
                        95.0m,
                        $"Order {order.OrderNumber.Value} was voided after cash tender was recorded. High-probability cash skimming theft pattern.",
                        details,
                        order.UpdatedAtUtc));
                }
            }
            // Scenario B: Specific lines voided on an order with cash tender
            else if (hasCashTender && voidedLines.Count > 0)
            {
                var fp = BuildFingerprint(CashierAnomalyType.ExcessiveVoidsAfterCashTender, shift.CashierName, shift.Id.Value, order.Id.Value, $"LineVoid-{order.OrderNumber.Value}");
                if (!existingFingerprints.Contains(fp))
                {
                    existingFingerprints.Add(fp);
                    var severity = voidAmountForOrder >= 500m ? AuditAlertSeverity.High : AuditAlertSeverity.Medium;
                    var riskScore = voidAmountForOrder >= 500m ? 78.0m : 55.0m;

                    var details = JsonSerializer.Serialize(new
                    {
                        OrderNumber = order.OrderNumber.Value,
                        VoidAmount = voidAmountForOrder,
                        VoidedLineCount = voidedLines.Count,
                        VoidedItems = voidedLines.Select(l => new { l.Quantity, l.UnitPrice, LineTotal = l.Quantity * l.UnitPrice }).ToList()
                    });

                    newAlerts.Add(CashierAuditAlert.Create(
                        shift.Id,
                        order.Id,
                        shift.CashierId,
                        shift.CashierName,
                        CashierAnomalyType.ExcessiveVoidsAfterCashTender,
                        severity,
                        riskScore,
                        $"Order {order.OrderNumber.Value} had {voidedLines.Count} item(s) voided (worth Rs. {voidAmountForOrder:N2}) after cash tender.",
                        details,
                        order.UpdatedAtUtc));
                }
            }
        }

        // Scenario C: Aggregate shift void rate anomaly
        if (totalLineCount >= 10)
        {
            var voidRate = (decimal)totalVoidedLines / totalLineCount;
            if (voidRate >= 0.15m) // > 15% void rate
            {
                var fp = BuildFingerprint(CashierAnomalyType.ExcessiveVoidsAfterCashTender, shift.CashierName, shift.Id.Value, null, $"ShiftVoidRate-{shift.ShiftNumber}");
                if (!existingFingerprints.Contains(fp))
                {
                    existingFingerprints.Add(fp);
                    var risk = Math.Min(95.0m, 70.0m + ((voidRate - 0.15m) * 100m));
                    var details = JsonSerializer.Serialize(new
                    {
                        ShiftNumber = shift.ShiftNumber,
                        TotalLines = totalLineCount,
                        VoidedLines = totalVoidedLines,
                        VoidRate = Math.Round(voidRate * 100m, 1),
                        TotalVoidAmount = totalVoidedAmount
                    });

                    newAlerts.Add(CashierAuditAlert.Create(
                        shift.Id,
                        null,
                        shift.CashierId,
                        shift.CashierName,
                        CashierAnomalyType.ExcessiveVoidsAfterCashTender,
                        AuditAlertSeverity.High,
                        risk,
                        $"Cashier {shift.CashierName} in Shift #{shift.ShiftNumber} had an excessive void rate of {voidRate:P1} ({totalVoidedLines}/{totalLineCount} items voided).",
                        details,
                        shift.ClosedAtUtc ?? DateTimeOffset.UtcNow));
                }
            }
        }
    }

    private async Task EvaluateExcessiveVoidsAcrossOrdersAsync(
        IReadOnlyList<Order> orders,
        HashSet<PaymentMethodId> cashMethodIds,
        HashSet<string> existingFingerprints,
        List<CashierAuditAlert> newAlerts,
        CancellationToken cancellationToken)
    {
        foreach (var order in orders)
        {
            var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var hasCashTender = payments.Any(p => cashMethodIds.Contains(p.PaymentMethodId) && !p.IsVoided);
            if (!hasCashTender) continue;

            var lines = (await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken)).ToList();
            var voidedLines = lines.Where(l => l.IsVoided).ToList();
            if (voidedLines.Count == 0 && order.Status != OrderStatus.Voided) continue;

            var voidAmount = voidedLines.Sum(l => l.Quantity * l.UnitPrice);
            var fp = BuildFingerprint(CashierAnomalyType.ExcessiveVoidsAfterCashTender, "Cashier", null, order.Id.Value, order.OrderNumber.Value);
            if (existingFingerprints.Contains(fp)) continue;

            existingFingerprints.Add(fp);
            var isFullVoid = order.Status == OrderStatus.Voided;
            var severity = isFullVoid ? AuditAlertSeverity.Critical : (voidAmount >= 500m ? AuditAlertSeverity.High : AuditAlertSeverity.Medium);
            var risk = isFullVoid ? 92.0m : (voidAmount >= 500m ? 75.0m : 55.0m);

            newAlerts.Add(CashierAuditAlert.Create(
                null,
                order.Id,
                null,
                "Cashier",
                CashierAnomalyType.ExcessiveVoidsAfterCashTender,
                severity,
                risk,
                $"Order {order.OrderNumber.Value} had void activity after cash payment tender (Voided: Rs. {voidAmount:N2}).",
                JsonSerializer.Serialize(new { OrderNumber = order.OrderNumber.Value, VoidAmount = voidAmount }),
                order.UpdatedAtUtc));
        }
    }

    private void EvaluateFrequentManagerOverrides(
        Shift shift,
        IReadOnlyList<ActivityLogEntry> logs,
        HashSet<string> existingFingerprints,
        List<CashierAuditAlert> newAlerts)
    {
        var shiftClose = shift.ClosedAtUtc ?? DateTimeOffset.UtcNow;
        var cashierLogs = logs
            .Where(l => l.OccurredAtUtc >= shift.OpenedAtUtc &&
                        l.OccurredAtUtc <= shiftClose &&
                        (string.Equals(l.PerformedBy, shift.CashierName, StringComparison.OrdinalIgnoreCase) ||
                         (l.Details != null && l.Details.Contains(shift.CashierName, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        var overrideLogs = cashierLogs
            .Where(l => l.Action.Contains("Override", StringComparison.OrdinalIgnoreCase) ||
                        (l.Details != null && (l.Details.Contains("manager", StringComparison.OrdinalIgnoreCase) ||
                                              l.Details.Contains("authorization", StringComparison.OrdinalIgnoreCase))))
            .ToList();

        if (overrideLogs.Count >= 3)
        {
            var fp = BuildFingerprint(CashierAnomalyType.FrequentManagerOverrides, shift.CashierName, shift.Id.Value, null, $"Overrides-{overrideLogs.Count}");
            if (!existingFingerprints.Contains(fp))
            {
                existingFingerprints.Add(fp);
                var severity = overrideLogs.Count >= 5 ? AuditAlertSeverity.High : AuditAlertSeverity.Medium;
                var risk = Math.Min(90.0m, 50.0m + ((overrideLogs.Count - 2) * 10m));

                var details = JsonSerializer.Serialize(new
                {
                    ShiftNumber = shift.ShiftNumber,
                    OverrideCount = overrideLogs.Count,
                    Actions = overrideLogs.Select(l => new { l.Action, l.Details, l.OccurredAtUtc }).ToList()
                });

                newAlerts.Add(CashierAuditAlert.Create(
                    shift.Id,
                    null,
                    shift.CashierId,
                    shift.CashierName,
                    CashierAnomalyType.FrequentManagerOverrides,
                    severity,
                    risk,
                    $"Cashier {shift.CashierName} accumulated {overrideLogs.Count} manager overrides during Shift #{shift.ShiftNumber}. Threshold: 3.",
                    details,
                    overrideLogs.Max(l => l.OccurredAtUtc)));
            }
        }
    }

    private void EvaluateUnlinkedDrawerOpenings(
        Shift shift,
        IReadOnlyList<Order> shiftOrders,
        IReadOnlyList<ActivityLogEntry> logs,
        HashSet<string> existingFingerprints,
        List<CashierAuditAlert> newAlerts)
    {
        var shiftClose = shift.ClosedAtUtc ?? DateTimeOffset.UtcNow;

        // Drawer open events from ActivityLog
        var drawerLogs = logs
            .Where(l => l.OccurredAtUtc >= shift.OpenedAtUtc &&
                        l.OccurredAtUtc <= shiftClose &&
                        (l.Action.Contains("Drawer", StringComparison.OrdinalIgnoreCase) ||
                         l.Action.Contains("No Sale", StringComparison.OrdinalIgnoreCase) ||
                         (l.Details != null && l.Details.Contains("Drawer Kick", StringComparison.OrdinalIgnoreCase))))
            .ToList();

        // Cash movements on shift
        var unlinkedMovements = shift.CashMovements
            .Where(m => m.Type == CashMovementType.CashOut ||
                        (m.Reason != null && m.Reason.Contains("Drawer", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var totalDrawerKicks = drawerLogs.Count + unlinkedMovements.Count;

        // Anomaly condition: Drawer opened multiple times without corresponding sales activity
        if (totalDrawerKicks >= 2)
        {
            var fp = BuildFingerprint(CashierAnomalyType.UnlinkedCashDrawerOpening, shift.CashierName, shift.Id.Value, null, $"DrawerKicks-{totalDrawerKicks}");
            if (!existingFingerprints.Contains(fp))
            {
                existingFingerprints.Add(fp);
                var severity = totalDrawerKicks >= 4 ? AuditAlertSeverity.High : AuditAlertSeverity.Medium;
                var risk = Math.Min(88.0m, 50.0m + (totalDrawerKicks * 8m));

                var details = JsonSerializer.Serialize(new
                {
                    ShiftNumber = shift.ShiftNumber,
                    UnlinkedOpenings = totalDrawerKicks,
                    DrawerLogs = drawerLogs.Select(l => new { l.Action, l.Details, l.OccurredAtUtc }).ToList(),
                    CashMovements = unlinkedMovements.Select(m => new { m.Type, m.Amount, m.Reason }).ToList()
                });

                newAlerts.Add(CashierAuditAlert.Create(
                    shift.Id,
                    null,
                    shift.CashierId,
                    shift.CashierName,
                    CashierAnomalyType.UnlinkedCashDrawerOpening,
                    severity,
                    risk,
                    $"Detected {totalDrawerKicks} unlinked cash drawer openings on Shift #{shift.ShiftNumber} by {shift.CashierName}.",
                    details,
                    shiftClose));
            }
        }
    }

    private async Task EvaluateUnusualDiscountClustersAsync(
        Shift shift,
        IReadOnlyList<Order> shiftOrders,
        HashSet<string> existingFingerprints,
        List<CashierAuditAlert> newAlerts,
        CancellationToken cancellationToken)
    {
        var shiftDiscounts = new List<(Order Order, Discount Discount)>();

        foreach (var order in shiftOrders)
        {
            var discounts = await discountRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var d in discounts)
            {
                shiftDiscounts.Add((order, d));

                // Check 1: Large magnitude individual discount
                bool isHighDiscount = (d.DiscountType == DiscountType.Percentage && d.Value >= 25m) ||
                                      (d.DiscountType == DiscountType.FixedAmount && d.Value >= 500m);

                if (isHighDiscount)
                {
                    var fp = BuildFingerprint(CashierAnomalyType.UnusualDiscountCluster, shift.CashierName, shift.Id.Value, order.Id.Value, $"HighDisc-{d.Value}");
                    if (!existingFingerprints.Contains(fp))
                    {
                        existingFingerprints.Add(fp);
                        var severity = d.Value >= 35m ? AuditAlertSeverity.High : AuditAlertSeverity.Medium;
                        var risk = d.Value >= 35m ? 78.0m : 58.0m;

                        newAlerts.Add(CashierAuditAlert.Create(
                            shift.Id,
                            order.Id,
                            shift.CashierId,
                            shift.CashierName,
                            CashierAnomalyType.UnusualDiscountCluster,
                            severity,
                            risk,
                            $"High discount of {d.Value}{(d.DiscountType == DiscountType.Percentage ? "%" : " Rs.")} applied on order {order.OrderNumber.Value}. Reason: '{d.Reason}'.",
                            JsonSerializer.Serialize(new { OrderNumber = order.OrderNumber.Value, DiscountAmount = d.Value, d.Reason }),
                            d.CreatedAtUtc));
                    }
                }
            }
        }

        // Check 2: Clustered discounts (e.g. 3 or more discounts applied within 30 minutes)
        if (shiftDiscounts.Count >= 3)
        {
            var orderedDiscounts = shiftDiscounts.OrderBy(x => x.Discount.CreatedAtUtc).ToList();
            for (int i = 0; i <= orderedDiscounts.Count - 3; i++)
            {
                var windowStart = orderedDiscounts[i].Discount.CreatedAtUtc;
                var windowEnd = orderedDiscounts[i + 2].Discount.CreatedAtUtc;
                if ((windowEnd - windowStart).TotalMinutes <= 30)
                {
                    var fp = BuildFingerprint(CashierAnomalyType.UnusualDiscountCluster, shift.CashierName, shift.Id.Value, null, $"DiscCluster-{windowStart.Ticks}");
                    if (!existingFingerprints.Contains(fp))
                    {
                        existingFingerprints.Add(fp);
                        newAlerts.Add(CashierAuditAlert.Create(
                            shift.Id,
                            null,
                            shift.CashierId,
                            shift.CashierName,
                            CashierAnomalyType.UnusualDiscountCluster,
                            AuditAlertSeverity.High,
                            80.0m,
                            $"Cashier {shift.CashierName} applied 3 discounts within a 30-minute window on Shift #{shift.ShiftNumber}. Suspected discount abuse / sweethearting.",
                            JsonSerializer.Serialize(new
                            {
                                ShiftNumber = shift.ShiftNumber,
                                ClusterCount = 3,
                                WindowMinutes = (windowEnd - windowStart).TotalMinutes,
                                Orders = orderedDiscounts.Skip(i).Take(3).Select(x => x.Order.OrderNumber.Value).ToList()
                            }),
                            windowEnd));
                    }
                    break;
                }
            }
        }
    }

    private static string BuildFingerprint(CashierAnomalyType type, string cashier, Guid? shiftId, Guid? orderId, string detail) =>
        $"{type}_{cashier.Trim().ToLowerInvariant()}_{shiftId}_{orderId}_{detail.Trim().ToLowerInvariant()}";
}
