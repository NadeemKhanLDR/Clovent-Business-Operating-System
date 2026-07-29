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

## Desktop screen

`EndOfDayReportView` (`src/Clovent.Desktop/Restaurant/EndOfDay/`): a Warehouse/Date picker
and Generate button, with one `XtraTabControl` page per report section — Summary (labels
plus a text-based Print via `ReceiptPrintDocument`, reused from the POS receipt-printing
gap-closing work) and four data grids (Items Sold, Cash Summary, Inventory Movement, Stock
Remaining), each with its own native DevExpress Preview/Print/Export PDF/Export Excel
actions (`GridControl.ShowPrintPreview()`/`ShowRibbonPrintPreview()`/`ExportToPdf()`/
`ExportToXlsx()` — confirmed present and working in the referenced `DevExpress.Win`
version, no new package required).

A single combined print document spanning every section (one PDF with the summary numbers
followed by all four tables) was considered and deliberately not attempted — DevExpress's
`CompositeLink`/multi-link `PrintingSystem` composition adds real complexity for uncertain
benefit at this MVP's scope, when every section is already independently
previewable/printable/exportable. Worth revisiting if a client specifically asks for one
combined report file.

Feature-gated per `endofday.view`; nav key `endofday`, menu permission `menu.endofday`.
