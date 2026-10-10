using System;
using System.Collections.Generic;

namespace Clovent.Restaurant.Application.CashierAudits.Dtos;

/// <summary>
/// Aggregated behavioral risk profile for a cashier across shifts and transactions.
/// Used for manager risk heatmaps and forensic ranking.
/// </summary>
public sealed record CashierRiskScoreDto(
    Guid? CashierId,
    string CashierName,
    decimal OverallRiskScore,
    string RiskLevel, // Low, Medium, High, Critical
    int TotalShiftsObserved,
    int TotalOrdersProcessed,
    decimal TotalSalesAmount,
    int ActiveAlertsCount,
    int ExcessiveVoidAlertsCount,
    int ManagerOverridesCount,
    int UnlinkedDrawerCount,
    int DiscountClusterCount,
    decimal TotalVoidAmount,
    decimal TotalDiscountAmount,
    IReadOnlyList<decimal> RiskTrendSparkline);
