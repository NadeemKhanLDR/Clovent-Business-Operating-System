using System;
using System.Collections.Generic;

namespace Clovent.Inventory.Application.Reports.Dtos;

/// <summary>Summary item in a menu item / recipe food cost report.</summary>
public sealed record RecipeCostReportItemDto(
    Guid ProductVariantId,
    string RecipeName,
    decimal YieldQuantity,
    string YieldUnit,
    decimal CostPerPortion,
    decimal SellingPrice,
    decimal FoodCostPercentage,
    decimal GrossProfitPerPortion,
    decimal GrossMarginPercentage,
    int IngredientCount,
    bool IsCostAlert);

/// <summary>Summary report of all active recipe food costs.</summary>
public sealed record RecipeCostReportDto(
    DateTimeOffset GeneratedAtUtc,
    int TotalRecipes,
    decimal AverageFoodCostPercentage,
    IReadOnlyList<RecipeCostReportItemDto> Items);

/// <summary>Item in the raw ingredient cost report.</summary>
public sealed record IngredientCostReportItemDto(
    Guid IngredientVariantId,
    string Sku,
    string IngredientName,
    string UnitOfMeasureCode,
    decimal CurrentUnitCost,
    decimal QuantityOnHand,
    decimal TotalStockValuation,
    int RecipesUsedInCount);

/// <summary>Comprehensive ingredient cost report.</summary>
public sealed record IngredientCostReportDto(
    DateTimeOffset GeneratedAtUtc,
    int TotalIngredients,
    decimal TotalStockValuation,
    IReadOnlyList<IngredientCostReportItemDto> Items);

/// <summary>Item in the theoretical vs actual ingredient consumption and variance report.</summary>
public sealed record StockVarianceItemDto(
    Guid IngredientVariantId,
    string Sku,
    string IngredientName,
    string UnitOfMeasureCode,
    decimal TheoreticalQuantity,
    decimal ActualQuantity,
    decimal VarianceQuantity,
    decimal UnitCost,
    decimal VarianceCost,
    string VarianceClassification);

/// <summary>Theoretical vs actual stock variance report.</summary>
public sealed record StockVarianceReportDto(
    Guid WarehouseId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalIngredientsEvaluated,
    decimal TotalTheoreticalCost,
    decimal TotalActualCost,
    decimal TotalVarianceCost,
    IReadOnlyList<StockVarianceItemDto> Items);

/// <summary>Item in an inventory valuation report.</summary>
public sealed record InventoryValuationItemDto(
    Guid WarehouseStockId,
    Guid WarehouseId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string UnitOfMeasureCode,
    decimal QuantityOnHand,
    decimal UnitCost,
    decimal TotalValuation,
    decimal MinimumStock,
    bool IsLowStock,
    bool IsNegativeStock);

/// <summary>Warehouse inventory asset valuation report.</summary>
public sealed record InventoryValuationReportDto(
    Guid? WarehouseId,
    DateTimeOffset GeneratedAtUtc,
    int TotalItems,
    int LowStockCount,
    int NegativeStockCount,
    decimal TotalAssetValuation,
    IReadOnlyList<InventoryValuationItemDto> Items);

/// <summary>Item in a kitchen wastage and spoilage report.</summary>
public sealed record WastageReportItemDto(
    Guid TransactionId,
    Guid WarehouseId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal EstimatedUnitCost,
    decimal EstimatedWastageCost,
    string Reason,
    DateTimeOffset OccurredAtUtc);

/// <summary>Kitchen wastage report.</summary>
public sealed record WastageReportDto(
    Guid? WarehouseId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalIncidents,
    decimal TotalWastageQuantity,
    decimal TotalWastageCost,
    IReadOnlyList<WastageReportItemDto> Items);
