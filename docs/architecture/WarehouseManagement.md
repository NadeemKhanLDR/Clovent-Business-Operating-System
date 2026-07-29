---
title: Warehouse Management Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Milestone 14
applies_to: src/Clovent.Desktop
---

# Warehouse Management Reference

Milestone 14 ("Product Catalog & Inventory Foundation") adds eleven management screens to `Clovent.Desktop` on top of Milestone 13's shared grid+toolbar infrastructure (`DesktopAdministration.md`): Product, Category, Brand, Unit, Barcode, Variant, Price, Warehouse Stock, Stock Adjustment, Stock Transfer, Inventory Transactions. This document covers what was extended in the shared infrastructure to support them and the screens' own design decisions.

---

## 1. Eleven screens, not twelve - `ProductGroup` has no dedicated screen

The milestone's own screen list names eleven screens; `ProductGroup` has full Domain/Application/Infrastructure layers (`CatalogArchitecture.md`) but no Desktop screen - the identical precedent `DesktopAdministration.md` Section 1 and `MasterData.md` already set for Language/TimeZone in Milestone 13 (full backend, no screen). Products can still be assigned a Group (the `ProductEditForm` combo populates from existing groups), just not created/renamed/deactivated through this milestone's UI.

---

## 2. Extending `MasterDataListView<TDto>`, not replacing it

Every one of the eleven screens is still built on `MasterDataListView<TDto>` (`Clovent.Desktop.MasterData`) - none of them needed a bespoke grid. Three additive, backward-compatible extensions were made to the shared control itself, all optional (every Milestone 13 screen's existing usage compiles and behaves identically unchanged):

**Extra action buttons (`MasterDataListAction<TDto>`).** Several Milestone 14 workflows need more than the fixed New/Edit/Activate/Deactivate set - `WarehouseStock`'s Receive/Issue/Reserve/Release, `StockAdjustment`'s Apply/Cancel, `StockTransfer`'s Complete/Cancel, `Barcode`'s Mark/Unmark Primary. A screen now passes an optional `IReadOnlyList<MasterDataListAction<TDto>>` to the constructor - each action is a caption, a per-row handler, an optional `IsEnabledFor` predicate (e.g. "Apply" only enabled while `Status == "Pending"`), and an optional `FeatureOperation` string gating it through the same `CanUseFeatureAsync` mechanism Edit/Activate/Deactivate already use.

**CSV export/import (`OnExportCsv`/`OnImportCsv`).** Two more optional delegate properties, each backing a toolbar button that only becomes visible once the screen sets it - a screen without a natural tabular export (an append-only ledger, a workflow entity with only two or three fields) simply never sets them, and the buttons stay hidden. Only `ProductManagementView` wires both, being the one screen where bulk export/import is genuinely useful; the reference-data screens (Category/Brand/Unit) were judged not to need it given their small scale, a deliberate scope call rather than an oversight.

**Feature-gating extended to cover the new action buttons.** Milestone 13's `UpdateFeaturePermissionsAsync` only ever checked `create`/`edit`/`activate`/`deactivate`. It now also resolves each extra action's `FeatureOperation` (when set) through the same `CanUseFeatureAsync` callback, storing the result on the button's own `Tag` - the same storage-then-read-in-`UpdateButtonStates` pattern already used for Edit/Activate/Deactivate, just generalized to an arbitrary list instead of three fixed fields.

---

## 3. Two new shared pickers: `EntityPicker` and `ComboBoxBinder`

**`EntityPicker`** is the flat-list counterpart to Milestone 13's `OrganizationHierarchySelector` - a single labelled combo scoping a list view to one parent, for screens whose scope is one level deep rather than a three-level Organization → Company → Branch cascade: Variant management scoped by Product, Barcode/Price management scoped by Variant, WarehouseStock/StockAdjustment/InventoryTransactions scoped by Warehouse. Unlike `OrganizationHierarchySelector`, it has no query knowledge of its own - the caller supplies `(Guid Id, string Display)` pairs directly via `LoadItems`, keeping the control reusable across every entity kind without a cascading-selector's per-level MediatR dependency.

**`ComboBoxBinder`** is the edit-dialog counterpart - a static helper binding a plain `ComboBoxEdit` (placed inside an entity's own `MasterDataEditFormBase`-derived dialog, alongside that dialog's own `AddField` label) to `(Guid Id, string Display)` pairs, with an optional "(none)" entry for nullable foreign keys. `ProductEditForm`'s Category/Group/Brand/Unit combos, `ProductVariantEditForm`'s Unit combo, `ProductPriceEditForm`'s Currency combo, and `StockTransferCreateForm`'s source/destination Warehouse combos all use it rather than five near-identical hand-rolled display-to-id dictionaries.

**A new query was needed to support the Warehouse picker.** `Clovent.MasterData.Warehouses.IWarehouseRepository` had no flat "every warehouse" method (only `GetByBranchIdAsync`, scoped) - Milestone 13 never needed one, since every Milestone 13 Warehouse-consuming screen drilled down the Organization/Company/Branch hierarchy first. Milestone 14's Inventory screens scope by warehouse directly, with no reason to make a user drill through three combos just to pick one. `GetAllAsync` was added to the repository interface and its EF Core implementation, backing a new `ListAllWarehousesQuery` in `Clovent.MasterData.Application` - a minimal, additive extension to a Milestone 13 project, exactly what this milestone's own task list anticipated ("extend Milestone 13's... infra as needed").

---

## 4. CSV import/export: a minimal, dependency-free helper

**`CsvFile`** (`Clovent.Desktop.Shared`) is a small RFC 4180-style reader/writer - `FormatRow`/`ParseRow` (quoting/escaping a single row) plus `Write`/`ReadDataRows` (the two file-path entry points). Kept as pure string-in/string-out logic wherever possible, the same "extract for testability" discipline `DesktopAdministration.md` Section 1 already applied to `MasterDataFilter` - `Clovent.Desktop.Tests/Shared/CsvFileTests.cs` exercises the escaping/round-trip logic without touching disk.

**Only `ProductManagementView` wires CSV import**, not every screen (Section 2). Export writes `Sku, Name, BaseUnitOfMeasureCode, TaxRatePercentage, TaxIsInclusive, Status`; import reads the same shape back, resolving the unit-of-measure code to an id via a client-side dictionary built from `ListUnitsOfMeasureQuery` (there is no "get unit by code" query surface to call instead) and skipping any row whose code doesn't resolve, reporting an imported/skipped count rather than failing the whole batch on one bad row.

---

## 5. Why some screens have fewer buttons than others

The same discipline `DesktopAdministration.md` Section 2 established - never a button wired to a command that doesn't exist:

- **Barcode** has no Edit button - `Barcode.Value` is immutable after creation (no rename method exists), only Mark/UnmarkAsPrimary and Activate/Deactivate.
- **StockAdjustment/StockTransfer** have no Edit button either - every field is fixed once proposed (`CatalogArchitecture.md`/`InventoryArchitecture.md`'s "propose-then-commit" reasoning); only Apply/Cancel or Complete/Cancel act on them afterward, as extra actions rather than a status toggle.
- **InventoryTransactions** has no New/Edit/Activate/Deactivate at all - it is a read-only ledger view; `InventoryTransaction` records are only ever created by the WarehouseStock/StockAdjustment/StockTransfer screens' own command handlers (`InventoryArchitecture.md` Section 2), never directly.
- **ProductPrice's Edit dialog disables PriceType and Currency** - both are immutable after creation (`ProductPrice` exposes no method to change either); only the Amount field is editable.

---

## 6. Verified: builds clean, tests pass

- `Clovent.Desktop`: 0 build warnings, 0 errors, all eleven screens + three shared-infrastructure extensions + five new Dashboard widgets + one new seed task.
- `Clovent.Desktop.Tests`: 73 tests (up from 58 pre-Milestone-14 - the delta is `CsvFileTests` and `CatalogDashboardCalculationsTests`, covering the two new pieces of pure logic this milestone extracted).

---

## 7. Open questions for Solution Architect review

1. **CSV import is wired for Product only** (Section 4) - a deliberate scope call given time and the milestone's own emphasis on Product as "the core used by Restaurant POS, Retail POS, Purchasing, Sales, Manufacturing." **Needs a decision**: does a future milestone need import for Category/Brand/Unit (bulk reference-data setup) or Variant/Price (bulk catalog population per product)?
2. **`EntityPicker` re-fetches its full option list on every screen load, with no caching** - the same "acceptable at demo/seed-data scale, worth revisiting at real scale" flag `DesktopAdministration.md` open question #1 already raised for `OrganizationHierarchySelector`, now applying to a second control of the same shape.
3. **The Inventory Value dashboard widget resolves each distinct variant's cost price with a separate query** (no flat "list every current price" surface exists) - flagged in `CatalogArchitecture.md`/`InventoryArchitecture.md` implicitly via the "no cross-entity referential integrity" open questions; explicitly here as a performance concern once a catalog holds more than a few dozen variants with stock.
