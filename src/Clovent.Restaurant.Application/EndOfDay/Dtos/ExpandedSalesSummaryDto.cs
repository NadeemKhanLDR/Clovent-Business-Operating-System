using System;
using System.Collections.Generic;

namespace Clovent.Restaurant.Application.EndOfDay.Dtos;

/// <summary>Summary KPIs for the sales reporting period.</summary>
public sealed record SalesSummaryKpiDto(
    decimal GrossSales,
    decimal Discounts,
    decimal ServiceCharges,
    decimal DeliveryFees,
    decimal NetSales,
    decimal Tax,
    decimal TotalCollected,
    decimal CashCollected,
    decimal CardCollected,
    decimal OtherTenderCollected,
    decimal OnAccountCreated,
    decimal CustomerPaymentsCollected,
    decimal ClosingReceivables,
    decimal CustomerAdvancesReceived,
    decimal CustomerAdvancesApplied,
    decimal DeliverySales,
    int TotalOrders,
    decimal AverageOrderValue,
    int VoidedOrdersCount,
    decimal VoidedOrdersAmount,
    int ShiftCount,
    decimal TotalShiftVariance,
    decimal ItemSales = 0m,
    decimal TotalBillSales = 0m)
{
    /// <summary>Compatibility alias for DeliveryFees.</summary>
    public decimal TotalDeliveryFees => DeliveryFees;
    /// <summary>Resolved item sales subtotal.</summary>
    public decimal ItemSalesValue => ItemSales > 0m ? ItemSales : GrossSales;
    /// <summary>Resolved total bill sales (including delivery/service fees).</summary>
    public decimal TotalBillSalesValue => TotalBillSales > 0m ? TotalBillSales : NetSales;
}

/// <summary>Order line item for master-detail expansion in Sales Summary.</summary>
public sealed record ExpandedOrderLineRowDto(
    string ItemName,
    string VariantName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Tax,
    decimal ServiceCharge,
    decimal LineTotal,
    string ItemType);

/// <summary>Order row detail for the expanded sales summary report.</summary>
public sealed record ExpandedOrderRowDto(
    Guid OrderId,
    string OrderNumber,
    int? DailySalesNumber,
    string OrderType,
    string OrderSource,
    string TableOrRider,
    string CustomerName,
    int ItemsCount,
    decimal Subtotal,
    decimal Discount,
    decimal ServiceAndDeliveryFee,
    decimal Tax,
    decimal Total,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string PaymentSummary,
    decimal PaidAmount = 0m,
    decimal OnAccountAmount = 0m,
    decimal OutstandingAmount = 0m,
    IReadOnlyList<ExpandedOrderLineRowDto>? Lines = null)
{
    /// <summary>Distinct line items count alias.</summary>
    public int LineItemsCount => ItemsCount;
}

/// <summary>Item performance row for the expanded sales summary report.</summary>
public sealed record ExpandedItemRowDto(
    Guid VariantId,
    string CategoryName,
    string ItemName,
    string ItemType,
    decimal QuantitySold,
    decimal UnitPrice,
    decimal CostPrice,
    decimal TotalSales,
    decimal? EstimatedCost,
    decimal? GrossProfit,
    decimal? MarginPercent,
    decimal PercentOfTotalSales);

/// <summary>Customer activity row for the expanded sales summary report.</summary>
public sealed record ExpandedCustomerRowDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string MobileNumber,
    int OrdersCount,
    decimal Quantity,
    decimal GrossSales,
    decimal Discount,
    decimal Fees,
    decimal NetSales,
    decimal TotalPaid,
    decimal OnAccountIncurred,
    decimal AccountPaymentsCollected,
    decimal EndingReceivable,
    decimal AdvanceBalance)
{
    /// <summary>Compatibility alias for item sales subtotal.</summary>
    public decimal ItemSales => GrossSales;
    /// <summary>Compatibility alias for total bill sales.</summary>
    public decimal BillTotal => NetSales;
}

/// <summary>Payment method performance row for the expanded sales summary report.</summary>
public sealed record ExpandedPaymentRowDto(
    string PaymentMethodName,
    int TransactionsCount,
    decimal TotalCollected,
    decimal PercentOfTotal,
    string Category = "Order Settlement");

/// <summary>Receivables activity row during the reporting period.</summary>
public sealed record ExpandedReceivableActivityRowDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    decimal OpeningReceivable,
    decimal NewOnAccountSales,
    decimal CustomerPayments,
    decimal AdvanceApplied,
    decimal ClosingReceivable,
    decimal OpeningAdvance,
    decimal AdvanceReceived,
    decimal AdvanceUsed,
    decimal ClosingAdvance);

/// <summary>Order type channel breakdown row.</summary>
public sealed record ExpandedOrderTypeBreakdownDto(
    string OrderType,
    int OrdersCount,
    decimal QuantitySold,
    decimal GrossSales,
    decimal Discount,
    decimal DeliveryFees,
    decimal NetSales,
    decimal AverageOrderValue,
    decimal PercentOfTotal)
{
    /// <summary>Compatibility alias for TotalSales -> NetSales.</summary>
    public decimal TotalSales => NetSales;
}

/// <summary>Item classification and food cost profitability breakdown row.</summary>
public sealed record ExpandedItemClassificationBreakdownDto(
    string ItemType,
    decimal QuantitySold,
    decimal NetSales,
    decimal? TotalCost,
    decimal? GrossProfit,
    decimal? MarginPercent,
    string CostDisplay)
{
    /// <summary>Compatibility alias for TotalSales -> NetSales.</summary>
    public decimal TotalSales => NetSales;
    /// <summary>Compatibility alias for CostDisplay.</summary>
    public string CostDisplayText => CostDisplay;
}

/// <summary>Cash drawer reconciliation row by shift for the Cash Summary tab.</summary>
public sealed record ShiftDrawerCashSummaryDto(
    Guid ShiftId,
    int ShiftNumber,
    string CashierName,
    decimal OpeningFloat,
    decimal CashSales,
    decimal CashCollections,
    decimal CashIn,
    decimal CashOut,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal Variance,
    string Status);

/// <summary>Root aggregated response of the Expanded Sales Summary query.</summary>
public sealed record ExpandedSalesSummaryDto(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate,
    SalesSummaryKpiDto Kpis,
    IReadOnlyList<ExpandedOrderRowDto> Orders,
    IReadOnlyList<ExpandedItemRowDto> Items,
    IReadOnlyList<ExpandedCustomerRowDto> Customers,
    IReadOnlyList<ExpandedPaymentRowDto> Payments,
    IReadOnlyList<ExpandedReceivableActivityRowDto> Receivables,
    IReadOnlyList<ExpandedOrderTypeBreakdownDto> OrderTypes,
    IReadOnlyList<ExpandedItemClassificationBreakdownDto> ItemTypes,
    IReadOnlyList<ShiftDrawerCashSummaryDto>? ShiftDrawers = null);
