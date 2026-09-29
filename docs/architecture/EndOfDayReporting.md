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

Feature-gated per `endofday.view`; nav key `endofday`, menu permission `menu.endofday`.
