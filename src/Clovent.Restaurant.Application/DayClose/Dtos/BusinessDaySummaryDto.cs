using System;
using System.Collections.Generic;

namespace Clovent.Restaurant.Application.DayClose.Dtos;

/// <summary>Summary representation of a shift included in business day closing.</summary>
public sealed record ShiftDayCloseItemDto(
    Guid ShiftId,
    int ShiftNumber,
    string CashierName,
    Guid TerminalId,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string Status,
    decimal StartingCash,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal CashVariance,
    string? VarianceReason,
    decimal CashRefunds = 0m,
    decimal CustomerAccountCreditRefunds = 0m);

/// <summary>Tax breakdown line item for business day close.</summary>
public sealed record DayCloseTaxItemDto(
    string Authority,
    string TaxCode,
    decimal RatePercentage,
    decimal TaxableBase,
    decimal TaxAmount);

/// <summary>Tender/payment-method breakdown item for business day close.</summary>
public sealed record DayCloseTenderItemDto(
    string PaymentMethodName,
    int TransactionCount,
    decimal TotalAmount);

/// <summary>Cashier sales performance item for business day close.</summary>
public sealed record DayCloseCashierSalesDto(
    string CashierName,
    int OrderCount,
    decimal TotalSales);

/// <summary>Order type sales distribution item for business day close.</summary>
public sealed record DayCloseOrderTypeSalesDto(
    string OrderType,
    int OrderCount,
    decimal TotalSales);

/// <summary>
/// Comprehensive summary of a business day for manager review prior to or after closing.
/// </summary>
public sealed record BusinessDaySummaryDto(
    Guid BranchId,
    DateOnly BusinessDate,
    bool IsAlreadyClosed,
    BusinessDayCloseDto? ExistingClose,
    IReadOnlyList<ShiftDayCloseItemDto> OpenShifts,
    IReadOnlyList<ShiftDayCloseItemDto> ClosedShifts,
    int TotalShifts,
    int TotalOrders,
    decimal TotalSales,
    decimal CashSales,
    decimal CardSales,
    decimal OtherSales,
    decimal Refunds,
    decimal Discounts,
    decimal Tax,
    decimal CashIn,
    decimal CashOut,
    decimal TotalShiftVariance,
    bool CanClose,
    string? BlockingReason = null,
    decimal CashRefunds = 0m,
    decimal CustomerAccountCreditRefunds = 0m,
    decimal OtherRefunds = 0m,
    // Sales Breakdown
    decimal GrossSales = 0m,
    decimal ItemDiscounts = 0m,
    decimal OrderDiscounts = 0m,
    decimal NetSales = 0m,
    int CancelledOrdersCount = 0,
    // Tax Breakdown
    decimal TaxableSales = 0m,
    decimal TaxExemptSales = 0m,
    IReadOnlyList<DayCloseTaxItemDto>? TaxBreakdown = null,
    // Payments Breakdown
    decimal BankTransferSales = 0m,
    decimal DigitalSales = 0m,
    int SplitPaymentsCount = 0,
    decimal SplitPaymentsTotal = 0m,
    decimal TotalCollections = 0m,
    decimal OutstandingReceivables = 0m,
    IReadOnlyList<DayCloseTenderItemDto>? TenderBreakdown = null,
    // Cash Drawer Reconciliation
    decimal OpeningFloat = 0m,
    decimal ExpectedClosingCash = 0m,
    decimal ActualClosingCash = 0m,
    // Operations Breakdown
    int CompletedOrdersCount = 0,
    int RefundedOrdersCount = 0,
    decimal AverageOrderValue = 0m,
    IReadOnlyList<DayCloseCashierSalesDto>? CashierBreakdown = null,
    IReadOnlyList<DayCloseOrderTypeSalesDto>? OrderTypeBreakdown = null);
