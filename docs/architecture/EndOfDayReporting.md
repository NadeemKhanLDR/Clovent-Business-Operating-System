# End-of-Day Reporting

Status: gap-closing pass, not a numbered milestone — added to close a confirmed gap in the
Client MVP audit (no Day-End/Z-report existed anywhere in the solution before this).

## What it is

`GetEndOfDayReportQuery(WarehouseId, Date)` in `Clovent.Restaurant.Application.EndOfDay`
computes the Day-End report for one warehouse on one calendar day (UTC): Today's Sales, Cash
Collected, Items Sold (ordered by quantity descending — doubles as Top Selling Items),
Cash Summary (grouped by payment method), Receipt Count, Voided Order Count (Transaction
Summary), and Average Sale.

Inventory Movement and Stock Remaining are **not** part of this DTO — the Desktop screen
(`EndOfDayReportView`) composes those directly from `Clovent.Inventory.Application`'s
existing `ListInventoryTransactionsByWarehouseQuery`/`ListWarehouseStocksByWarehouseQuery`
rather than this query re-wrapping data another query already exposes.

## Known limitations (same class as `Dashboard.md`'s existing ones)

- **Walks every order in memory.** `IOrderRepository.GetAllAsync()` then filters by
  warehouse/status/date client-side — the same "no batched read model yet" pattern
  `Dashboard.md` already documents for Today's Sales/Top Selling/Inventory Value. Fine at
  this MVP's demo scale; a real concern once order volume grows.
- **Cash Collected/Cash Summary match by `PaymentMethod.Name` string ("Cash",
  case-insensitive).** `PaymentMethod` has no typed Cash/Card distinction (see
  `RestaurantPOSArchitecture.md`), so this is a fragile string match, not a guaranteed
  correct one if an admin renames or duplicates a "Cash" method. A future milestone should
  add a `PaymentMethodKind` enum if cash-drawer reconciliation needs to be exact.
- **Total Sales is the sum of non-voided payments**, not a recomputed `OrderTotalsCalculator`
  grand total — mathematically equivalent for any order that reached `Completed` (the
  domain requires `Balance <= 0.005` to complete), but relies on that invariant holding.

## Expanded Sales Summary Architecture & High-DPI Stabilization Pass

The Sales Summary reporting has been expanded into a comprehensive 11-tab Day-End / Z-Report (`GetExpandedSalesSummaryQuery`) and Customer Receivables Aging Report (`GetCustomerReceivablesReportQuery`):
1. **Summary KPIs**: Gross Sales, Discounts, Service Charges, Delivery Fees, Net Sales, Taxes, Total Collections, AR created vs. collected, and Shift Variances.
2. **Orders/Bills Tab**: Master-detail grid showing orders and expandable item lines.
3. **Items Performance Tab**: Item quantities, sales, food cost calculations, gross profit, and margin percentages.
4. **Customers Activity Tab**: Breakdown of sales, discounts, credit purchases, and advance balances per customer.
5. **Payments Tab**: Breakdown of collections across cash, card, online, and account settlement.
6. **Receivables Movement Tab**: Opening receivables, new on-account sales, payments, advances applied, and closing receivables.
7. **Order Types Tab**: Channel breakdown across Dine-In, Takeaway, and Delivery with delivery fee tracking.
8. **Item Types / Profitability Tab**: Margins and food cost breakdown across `Prepared`, `PurchasedResale`, and `Service` items.
9. **Cash Summary Tab**: Drawer cash reconciliation, shift variances, and cash movements.
10. **Inventory Movement Tab**: Direct warehouse inventory receipts, issues, and adjustments.
11. **Stock Remaining Tab**: Current warehouse quantities on hand and reorder levels.

### High-DPI Layout Architecture (250% Scaling / 240 DPI)
WinForms `AutoScaleMode` is intentionally disabled in CBOS forms and user controls. DevExpress skins scale fonts according to DPI (e.g. 2.5x font size at 240 DPI). Hardcoded pixel widths without `DesktopDpi.Scale(...)` cause severe horizontal clipping, and setting `AutoHeight = false` on DevExpress editors prevents font-driven height calculation, causing vertical text clipping.
- **Customer Receivables Filter Strip**: Converted from `FlowLayoutPanel` to a 7-column `TableLayoutPanel` with DPI-scaled explicit column widths and minimum sizes (`ScaleLayoutAtRuntime()`), preventing clipping of As of Date, Filter dropdown, Search edit, and Refresh button.
- **Sales Summary Header Strip**: Replaced fixed 32px height and `AutoHeight = false` with DPI-scaled font-driven editor heights, and scaled buttons and GridView row heights (`RowHeight = 28 scaled`, `ColumnPanelRowHeight = 32 scaled`).

### Realistic Data Seeding & In-Memory Stock Tracking
`DevelopmentRestaurantReportingSeedStartupTask` creates real database transactions:
- Real catalog entities for `Prepared` (Chicken Biryani), `PurchasedResale` (Naan), and `Service` (Food Heating).
- Real inventory transactions: 100 units of Naan received into the warehouse, and 30 units issued upon order completion across 12 orders (11 completed, 1 voided), leaving 70 units remaining in stock.
- Cross-reconciles with `GetExpandedSalesSummaryQuery` and `GetCustomerReceivablesReportQuery` with 100% mathematical fidelity.

### Sales Summary KPI & Footer Semantics

To ensure financial accuracy and eliminate misleading calculations in the Day-End / Sales Summary report:
1. **Total Sales Card Subtitle**:
   - Updated from `"Gross sales across all tenders"` to `"Completed bill sales including fees"` to accurately reflect that Total Sales includes all completed order sales with applicable fees (e.g. delivery fees).
2. **Cash KPI Clarification**:
   - Renamed from `"CASH COLLECTED"` to `"CASH SALES"` with subtitle `"Cash order settlements"`. This explicitly distinguishes POS counter order settlements from physical customer A/R cash collections.
3. **Items Performance Grid Footer (Unknown vs Zero Cost & Suppression of Averages/Percentages)**:
   - Excluded average and unit price columns (`UnitPrice`, `CostPrice`, `AverageOrderValue`) as well as percentages (`MarginPercent`, `PercentOfTotalSales`, `PercentOfTotal`, `Percent`) from automatic column sum footers (`ExcludedFromSumSummary`). Summing averages or percentages across disparate items is mathematically invalid.
   - Cost Amount (`EstimatedCost`, `TotalCost`) and Gross Profit (`GrossProfit`) use DevExpress custom summaries (`SummaryItem.SummaryType = SummaryItemType.Custom`, `DisplayFormat = "{0}"`).
   - In `CustomSummaryCalculate`, rows with null cost (e.g. Prepared dishes without recipe BOM) are skipped from cost and GP aggregation. Rows with legitimate zero cost (e.g. Services configured with zero direct cost) are included with 0.00 cost and 100% margin.
   - When zero costed rows exist in the filtered dataset, the Cost Amount footer renders `"N/A"` (or blank) and Gross Profit renders `"Known GP: N/A"`, avoiding misleading `"0.00"` / `"Known GP: 0.00"` labels.
   - When a mixed dataset is displayed (Prepared with unknown cost + Resale with known cost + Service with 0 cost), the footer sums only known-cost rows and displays `"Known GP: {amount}"`.
4. **Prepared Item Cost Transparency**:
   - When a prepared item lacks configured recipe/BOM costing, its unit cost is represented as `null` in the application layer and formatted as `"N/A"` in the UI grid, rather than displaying a deceptive `"0.00"` cost or artificial 100% margin.
5. **Print, Preview & Export Consistency**:
   - By assigning formatted text (`"N/A"`, `"Known GP: N/A"`, `"Known GP: {sum}"`) directly to `e.TotalValue` in `CustomSummaryCalculate`, Print Preview (`PrintableComponentLink`), PDF export, and WYSIWYG Excel export (`ExportType.WYSIWYG`) accurately reflect the exact same custom footer text without turning null cost summaries into `0.00`.
6. **High-DPI Workspace Scrollbar Elimination**:
   - `AutoScroll` and `AutoScrollMinSize` are explicitly disabled on `EndOfDayReportView`, child tab pages, and the root `MainForm`. GridView uses `ColumnAutoWidth = true`, preventing unnecessary application/workspace-level horizontal scrollbars at 1920x1080 @ ~250% scaling.

### Development Reporting Acceptance Dataset Specification

To enable comprehensive manual reporting verification on a single business date without requiring manual transaction manufacturing:
- **REPORT ACCEPTANCE DATE**: `01-Oct-2026`
- **REPORT ACCEPTANCE RUN ID**: `RUN-20261001-DEV-ACCEPTANCE`
- **Startup Task**: `DevelopmentRestaurantReportingSeedPhase2StartupTask`
- **Execution Mechanism**: Legitimate domain aggregates and CQRS application commands (MediatR), zero direct SQL insertion.
- **Idempotency**: Bound to configured business date (`IBusinessDateProvider`); skips execution if already clean on today's business date; automatically resets and provisions on date advancement.

#### Supported Reporting Scenarios Provisioned:
1. **Prepared Item Sale**: Chicken Biryani, Chicken Karahi, Chicken Haleem. Classified as `Prepared`. Quantity > 0, Sales > 0, Cost = `"N/A"` (unconfigured recipe/BOM).
2. **Purchased / Resale Item**: Naan (`PurchasedResale`). Purchase cost Rs.20, selling price Rs.25. 100 received via Goods Receipt (`GRN-2026-10-001`), issued upon POS sales. Margin = 20.0%, GP = Rs.5/unit.
3. **Service Item**: Food Heating (`Service`). Sales > 0, direct cost = 0, Gross Profit = 100%, zero physical inventory movement.
4. **Prepaid / Customer Advance**: Customer D overpays Rs.700 on Rs.475 On Account sale -> Rs.475 applied to A/R, Rs.225 advance credit balance created. Subsequent order `ORD-RPT-S2-006B` consumes Rs.225 advance + Rs.225 cash.
5. **Tenders Covered**: Cash, Credit Card, Mobile Wallet, On Account, Customer Advance, Split Payment (`ORD-RPT-S2-003`: 250 Cash + 250 Mobile Wallet).
6. **Collections**: Partial payments (Customer C: Rs.800 + Rs.500) and bulk collection batch `RCV-BATCH-S2-001` across 3 accounts (Cust B Rs.500 Cash, Cust C Rs.200 Wallet, Corporate Account Rs.300 Cash).
7. **Credit Limit Override**: Customer F (Credit Limit Rs.200) purchases Rs.500 On Account via authorized manager override (`ExceedCreditLimitApproved: true`).
8. **Discounted Transaction**: `ORD-RPT-S2-011` with Rs.50 fixed discount applied via `ApplyDiscountToOrderCommand` (Gross Rs.700, Discount Rs.50, Net Rs.650 Cash).
9. **Price Override**: `ORD-RPT-S2-012` Chicken Biryani overridden from Rs.450 to Rs.400 with supervisor audit reason (`line.OverridePrice`).
10. **Order & Line Customization Notes**: `ORD-RPT-S2-013` with order notes ("Pack separately - customer travelling"), customer notes, and item-level notes ("Extra spicy, no raita").
11. **Portion / Variant Reconciliation**: `ORD-RPT-S2-010` with Chicken Haleem Half Plate (Rs.260) and Full Plate (Rs.420).
12. **Fulfillment Workflows**: Dine-In (Tables T-01, T-02, T-03), Take Away, and Delivery (`ORD-RPT-S2-007` with Delivery Fee Rs.150, Rider "Kamran Rider", Rider Phone "0300-9876543").
13. **Order Lifecycle Non-Sales**: Held order (`ORD-RPT-S2-HELD`), Running order (`ORD-RPT-S2-RUN` on Table T-01 with Kitchen Ticket), and Voided order (`ORD-RPT-S2-VOID` with reason). Excluded from completed sales totals.
14. **Inventory Movements**: Opening baseline 70, Goods Receipt +100, Positive audit adjustment +10, Negative damage adjustment -5, Warehouse transfer out -10, POS sales issues -29 -> Closing stock 136 on WH-01.
15. **Shift Drawer Reconciliation**: Dedicated test shift closed with 0 variance (Starting float Rs.4,000 + Cash In Rs.1,000 + Cash Sales Rs.2,765 + Cash Collections Rs.2,800 - Cash Out Rs.500 = Counted Rs.10,065, Variance Rs.0.00). Live shift opened with float Rs.4,000 for immediate user testing.

Feature-gated per `endofday.view`; nav key `endofday`, menu permission `menu.endofday`.

### High-DPI Live-UI Polish & Reconciliation Pass (1920×1080 @ 240 DPI / ~250% Scaling)

A dedicated LIVE-UI polish pass was executed to resolve display and formatting defects identified under real high-DPI desktop scaling:

1. **Toolbar Single-Row Layout & Zero-Wrapping Guarantee**:
   - The Sales Summary filter bar was reconstructed from a wrapping `FlowLayoutPanel` into a structured 1-row, 10-column `TableLayoutPanel` docked to top.
   - Exact toolbar control and tab sequence:
     1. `Location` (`_warehousePicker`): Compact combo width 140px (~180–200 logical px container), supports full warehouse names with dropdown tooltips and text ellipsis.
     2. `Period` (`_periodCombo`): Compact preset selector (110 logical px).
     3. `From` (`_fromDateEdit`): Compact date editor (115 logical px).
     4. `To` (`_toDateEdit`): Compact date editor (115 logical px).
     5. `Generate` (`_generateButton`): 80 logical px.
     6. `Preview` (`_previewButton`): 70 logical px.
     7. `Print` (`_printButton`): 65 logical px.
     8. `Export PDF` (`_exportPdfButton`): 80 logical px.
     9. `Export Excel` (`_exportExcelButton`): 85 logical px.
     10. `Print Summary` (`_printSummaryButton`): 95 logical px, strictly positioned immediately following `Export Excel`.
   - Guaranteed zero wrapping onto a second row at 1920×1080 / 240 DPI.

2. **Monetary Formatting & Decimal Precision**:
   - Resolved raw SQL `decimal(18,6)` scale leak (e.g. `4600.000000`) in Customers tab by registering `GrossSales`, `ItemSales`, `BillTotal`, `StartingCash`, `ExpectedCash`, `CountedCash`, `Variance`, `CashVariance`, `CreditLimit` in `MoneyFieldNames`.
   - Configured DevExpress GridView columns with explicit `DisplayFormat.FormatType = FormatType.Numeric`, `DisplayFormat.FormatString = "n2"`, and `HAlignment = HorzAlignment.Far`.
   - All monetary figures consistently render with two decimal places (e.g. `4,600.00`) across all 10 report tabs.

3. **Cash Summary Open-vs-Closed Shift Semantics**:
   - For open/active cash register sessions (e.g. Shift #1009), the cashier has not counted the drawer.
   - Refactored `ShiftDrawerCashSummaryDto.CountedCash` and `Variance` to nullable `decimal?`.
   - Query handler sets `CountedCash = null` and `Variance = null` when `Shift.Status != ShiftStatus.Closed`.
   - GridView formats nulls as `"N/A"` via `CustomColumnDisplayText`, preventing misleading negative variances equal to `0 - ExpectedCash`.
   - Footer summaries: Meaningless arithmetic SUM on `ShiftNumber` (which summed shift IDs into fake numbers like 2010) was eliminated; column displays the standard `"Total"` row header.

4. **Inventory Movement Footer Integrity**:
   - In mixed inventory movements (Receipts, Issues, Adjustments, Transfers), summing raw positive quantities yields an invalid aggregate.
   - Explicitly cleared `SummaryItem` on `_inventoryMovementGridView.Columns["Quantity"]` to prevent deceptive totals.

5. **Upsell Performance High-DPI Rebalancing & Caption Preservation**:
   - Reconstructed column proportions to eliminate grid monopolization by `Recommended Item` (previously taking 60–70%):
     - `Recommended Item`: 40.0% (target 38–42%)
     - `Variant`: 13.0% (target 12–14%)
     - `Offers`: 6.5% (target 6–7%)
     - `Accepted`: 8.5% (target 8–9%)
     - `Dismissed`: 8.5% (target 8–9%)
     - `Conversion %`: 10.5% (target 10–11%)
     - `Upsell Revenue`: 13.0% (target 11–13%)
   - Wrapped column configuration in `_gridView.BeginUpdate()` / `_gridView.EndUpdate()` to prevent cumulative layout distortion on interactive addition.
   - Set safe `MinWidth` computed from `TextRenderer.MeasureText(caption, headerFont).Width + glyphPadding` preventing high-DPI caption truncation at 240 DPI (~250% scaling).
   - Disabled header filter buttons (`OptionsFilter.AllowFilter = false`) to preserve maximum text area.
   - Full captions (`Accepted`, `Dismissed`, `Conversion %`, `Upsell Revenue`) display without ellipsis.
   - Text columns left-aligned (`HorzAlignment.Near`), numeric columns right-aligned (`HorzAlignment.Far`).

6. **Customer Financial Reconciliation & Export Parity**:
   - The Customers report tab and printouts visibly explain how `Item Sales` becomes `Bill Total` across every customer row and footer summary:
     $$\text{Item Sales} - \text{Discount} + \text{Fees} + \text{Tax} = \text{Bill Total}$$
     $$\text{Paid} + \text{On Account} = \text{Bill Total}$$
   - Added `Discount` and `Tax` columns to the Customers grid view (`_customersGridView`) and `ExpandedCustomerRowDto`.
   - Acceptance dataset reconciliation:
     - Walk-in Guest: $\text{Item Sales } (6,020.00) - \text{Discount } (50.00) + \text{Fees } (0.00) + \text{Tax } (0.00) = \text{Bill Total } (5,970.00)$
     - Corporate Client: $\text{Item Sales } (4,925.00) - \text{Discount } (0.00) + \text{Fees } (150.00) + \text{Tax } (0.00) = \text{Bill Total } (5,075.00)$
     - Footer Totals: $\text{Item Sales } (10,945.00) - \text{Discount } (50.00) + \text{Fees } (150.00) + \text{Tax } (0.00) = \text{Bill Total } (11,045.00)$
     - Settlement Totals: $\text{Paid } (7,070.00) + \text{On Account } (3,975.00) = \text{Bill Total } (11,045.00)$
   - Full parity across On-Screen Report, Print Preview, Print (`ReceiptPreviewForm` and `GridReportingPrintService`), PDF export, and WYSIWYG Excel export.
   - All financial columns display formatted to two decimal places (`n2`).

7. **Shell Ribbon Group Caption Protection**:
   - Set `AllowTextClipping = false` on `RibbonPageGroup` within `MainForm.Designer.cs` so groups such as `"Financial / A/R"` are never truncated into `"Financial / A..."`.

8. **Single Consolidated Database Architecture**:
   - The system operates against a single unified SQL Server database (`Clovent_BusinessOperatingSystem`).
   - Bounded contexts are cleanly segregated by SQL schemas: `Authentication`, `Identity`, `MasterData`, `Catalog`, `Inventory`, `Restaurant`.
   - Each module maintains its own isolated migrations history table: `[Schema].[__EFMigrationsHistory]`.
   - All DbContexts, services, and repositories resolve the canonical connection string `ConnectionStrings:Default`.

### High-DPI (240 DPI / ~250% Scale) Live Quality Pass (Sales Summary & Stock On Hand)

A focused quality pass addressed real-world display truncation and data projection under high DPI (1920×1080 display, ~250% scaling):

1. **Sales Summary Location Editor Truncation**:
   - Increased logical combo width from 140px to 210px (`_warehousePicker.SetCustomWidth(210, 210)`), with DPI-scaled runtime sizing (`DesktopDpi.Scale(210, this)`).
   - Removed inner `_layout` padding (`Padding.Empty`) on `EntityPicker` to maximize text display area.
   - Long warehouse names such as `"Kitchen Backup Warehouse"` render completely without ellipsis (`...`) under 240 DPI while maintaining compact proportions.

2. **Sales Summary Label Vertical Centering**:
   - Replaced `FlowLayoutPanel` wrappers for `periodPanel`, `fromPanel`, `toPanel`, and the internal layout of `EntityPicker` with structured 1-row, 2-column `TableLayoutPanel` controls.
   - Set row size to `100%` and `Anchor = AnchorStyles.Left` with zero vertical margin offsets.
   - All labels (`Location:`, `Period:`, `From:`, `To:`) vertically center precisely relative to their companion editors without arbitrary `Location.Y` pixel patches.

3. **Sales Summary Action Button Uniform Sizing**:
   - Standardized all 6 action buttons (`Generate`, `Preview`, `Print`, `Export PDF`, `Export Excel`, `Print Summary`) to uniform logical dimensions: `110px` width × `30px` height.
   - Configured identical font (`Segoe UI 9pt Bold`), uniform padding `(8, 4, 8, 4)`, and uniform margins `(2, 0, 2, 0)`.
   - Enforced runtime scaling in `ScaleLayoutAtRuntime()`.
   - The entire toolbar remains strictly on a single row at high DPI in the exact canonical sequence: Location -> Period -> From -> To -> Generate -> Preview -> Print -> Export PDF -> Export Excel -> Print Summary.

4. **Stock On Hand Top Layout & Action Preservation**:
   - Replaced fragmented multi-row top panels with a clean, single-row `headerPanel` (`TableLayoutPanel`) containing `_warehousePicker` and `_receiveInventoryButton` side-by-side (`Warehouse: [ Kitchen Backup Warehouse ]  [ Receive Inventory ]`).
   - Resolved clipping of the "Receive Inventory" button (`ceive Invento`) by enabling `AutoSize = true` with `MinimumSize = (130, 28)`, bold font, and comfortable padding.
   - Retained canonical `Receive Inventory` quick-action on the warehouse stock view while preserving full sidebar navigation parity.

5. **Stock On Hand SKU & Product Identity Resolution**:
   - *Root Cause*: `LoadLookupsAsync` previously loaded warehouses into `_warehousePicker` before `ListProductVariantsQuery` completed. Selecting the first warehouse triggered an immediate asynchronous `RefreshAsync()`, which mapped rows before `_variantsById` was populated, resulting in permanently blank SKU and Product cells.
   - *Fix*: `LoadLookupsAsync` now fetches variants and builds `_variantsById` before binding warehouses. A defensive fallback in `LoadItemsAsync` ensures variants are loaded if cache is empty.
   - *Data Enrichment*: Enriched `ProductVariantDto` and `ListProductVariantsQuery` with the parent `ProductName` from `Catalog.Products`. Rows now display the full identity: `${ProductName} - ${VariantName}` (e.g. `Naan - Standard`) alongside the SKU (`NAAN-STD`).



