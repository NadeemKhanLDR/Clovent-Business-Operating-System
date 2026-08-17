---
title: Restaurant POS Architecture Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Demo Readiness Pass (terminology, click-reduction, error messages, empty states)
applies_to: src/Clovent.Restaurant, src/Clovent.Restaurant.Application, src/Clovent.Restaurant.Infrastructure, src/Clovent.Desktop
---

# Restaurant POS Architecture Reference

> **Note (2026-08-01):** The Desktop shell this document's Section 12 refers to (`ShellForm`, single-panel workspace) has been replaced by a `MainForm` + `DocumentManager`/`TabbedView` multi-document shell - see `DesktopShellArchitecture.md`. `RestaurantPosView` and every other Restaurant screen/dialog listed here are unaffected functionally (no Domain/Application/Infrastructure change) and are tracked, unconverted, in that document's Section 9 backlog; they continue to work as documents hosted in the new shell exactly as they did in the old one.

Milestone 15 ("Restaurant POS Core") introduces `Clovent.Restaurant`, a new bounded context covering Dine In/Take Away order-taking, table management, kitchen tickets, and payment - explicitly *not* Accounting, Online Ordering, Kitchen Display, Delivery, or Loyalty, all deferred to later milestones per the brief. Nine aggregates: `DiningArea`, `Table`, `Order`, `OrderLine`, `KitchenTicket`, `Payment`, `PaymentMethod`, `Discount`, `ServiceCharge`.

---

## 1. Cross-context references, and why Restaurant depends on Identity, MasterData, and Catalog

`Clovent.Restaurant` references `Clovent.Identity` (`BranchId`, for `DiningArea`), `Clovent.MasterData` (`WarehouseId` on `Order`, and `EntityCode` reused for `Table.Code` since the dependency already exists), and `Clovent.Catalog` (`ProductVariantId` on `OrderLine`) - always by strongly-typed id only, the same pattern every prior bounded-context boundary in this solution follows. `Clovent.Restaurant.Application` additionally depends on `Clovent.Catalog.Application` and `Clovent.Inventory.Application`, consumed only through `IMediator.Send` against their existing queries/commands - never their repositories or domain aggregates directly. This is the literal implementation of the milestone's own instruction ("Consume existing Inventory module... Do NOT duplicate inventory logic"), extended to Catalog for the identical reason: pricing and tax configuration already exist there.

---

## 2. `Order` deliberately holds no computed total

`Order.OrderLineIds`/`DiscountIds`/`ServiceChargeIds`/`PaymentIds` are `HashSet<T>`-backed id-lists, exposed as `IReadOnlyCollection<T>` - the identical shape `Organization.CompanyIds` established in Milestone 13. The aggregate tracks *which* lines/discounts/charges/payments belong to it and enforces its own status machine; it never computes a subtotal, tax total, or balance.

**`OrderTotalsCalculator` (`Clovent.Restaurant.Application.Orders`) is a pure, static function** taking already-loaded `OrderLineDto`/`DiscountDto`/`ServiceChargeDto`/`PaymentDto` collections and returning an `OrderTotals` record (Subtotal, TaxTotal, DiscountTotal, ServiceChargeTotal, GrandTotal, PaidTotal, Balance) - no repository, no database, unit-testable in isolation (`OrderTotalsCalculatorTests`). This is the same "cross-aggregate consistency is the handler's job" reasoning `InventoryArchitecture.md` Section 2 documents for `WarehouseStock`/`InventoryTransaction`, generalized to four collections instead of two.

**Tax-inclusive and tax-exclusive lines are not summed the same way.** `Subtotal` is `Quantity * UnitPrice` across active lines regardless of tax treatment - a tax-inclusive line's tax is already embedded in that number. `TaxTotal` (an informational figure for the Tax Summary widget) sums every active line's tax, inclusive or exclusive alike. `GrandTotal` is `Subtotal - DiscountTotal + ServiceChargeTotal + ExclusiveTaxAddOn`, where `ExclusiveTaxAddOn` is only the tax-exclusive lines' tax - adding a tax-inclusive line's tax a second time on top of `Subtotal` would double-count it. `CompleteOrderCommandHandler` calls the same calculator to verify `Balance <= 0.005m` (a small epsilon for rounding) before allowing completion; `Order.Complete()` itself does not check this - the same split GetOrderSummaryQuery/CompleteOrderCommand both rely on, one function backing both a read and a write path.

---

## 3. `OrderLine` is its own aggregate, and snapshots price/tax at add-time

`OrderLine` is not embedded in `Order` - it is a separate aggregate with its own repository, following the milestone's own explicit aggregate list and the precedent `ProductVariant`/`Barcode`/`ProductPrice` already set (each its own aggregate rather than a child collection of `Product` in `CatalogArchitecture.md`). This is what makes `TransferToOrder(OrderId)` possible for Table Split/Merge without touching the `Order` aggregates beyond their id-lists.

**`AddOrderLineCommandHandler` snapshots `UnitPrice`, `TaxRatePercentage`, and `TaxIsInclusive` onto the line at creation time**, resolved via `Clovent.Catalog.Application`'s `GetProductVariantByIdQuery`/`GetProductByIdQuery`/`ListProductPricesByVariantQuery` (picking the newest Active Selling price). If the catalog's price or tax configuration changes later, an already-placed order's historical total does not silently shift - the same reasoning a financial ledger requires for any line item.

**`OrderNumber` is generated from `DateTimeOffset.UtcNow.UtcTicks` (hex-encoded), not a database sequence** - avoids needing a dedicated sequence-number aggregate for a value that only needs to be unique and roughly chronological, not gapless.

---

## 4. Table Merge/Split/Transfer are Application-layer compositions, not new domain states

`OrderStatus` stays small (Open, Held, Completed, Voided, Cancelled) - no `Merged` or `Split` state was added. Instead:

- **`TransferOrderTableCommand`** reassigns `Order.TableId` (via `Order.AssignTable`), vacates the old table, occupies the new one.
- **`MergeTablesCommand`** moves every line from the source table's order to the target table's order (creating one there if none exists yet), then cancels the now-empty source order and vacates its table.
- **`SplitOrderCommand`** creates a new order at a target table, moves the selected `OrderLineId`s onto it via `OrderLine.TransferToOrder`, and removes them from the source order - the source order keeps whichever lines were not selected.

None of these three commands changes `Order`'s status vocabulary; they compose the existing one-way transitions (Cancel) and the mutable `OrderLine.OrderId` that already exists for exactly this purpose. See `OrderLifecycle.md` for the full transition diagram and worked examples.

**Split Bill has no dedicated command at all.** It is a natural consequence of `Payment` being its own aggregate an order can accumulate several of - `RecordPaymentCommand` called multiple times against the same order (different amounts, possibly different `PaymentMethod`s) *is* a split bill. The same "let the data model imply the feature" reasoning `CatalogArchitecture.md` already applied to `ProductPrice` supporting "multiple prices" without a dedicated multi-price command.

---

## 5. Reopen is two-way, unlike every other one-way transition elsewhere in this solution

Every prior "propose then commit" or "close" transition in this solution (`FiscalYear.Close()`, `StockAdjustment`/`StockTransfer` Apply/Complete, `InventoryArchitecture.md` Section 3) is explicitly one-way - a mistake is corrected by a new, opposite record, never by undoing the original. `Order.Reopen()` breaks that pattern deliberately: Voided/Cancelled → Open is two-way, because the milestone's own capability list names "Reopen Order" as a required operation, not an oversight. `ReopenOrderCommandHandler` also re-seats the order's table if it isn't already occupied, undoing the Vacate that Void/Cancel performed.

---

## 6. Inventory integration: consuming `IssueStockCommand`, never duplicating it

`CompleteOrderCommandHandler` is the one place Restaurant touches Inventory. For every active (non-voided) line, it calls `mediator.Send(GetWarehouseStockByWarehouseAndVariantQuery(order.WarehouseId, line.ProductVariantId))`; if a stock record exists, it calls `mediator.Send(IssueStockCommand(stock.WarehouseStockId, line.Quantity, ...))`. **If no `WarehouseStock` record exists for that warehouse/variant pair, the line is silently skipped** - a deliberate judgment call favoring POS usability for made-to-order or untracked items (a kitchen-prepared side dish that was never given a warehouse balance shouldn't block checkout), documented here and in `OrderLifecycle.md` rather than left as an unexplained silent branch.

No `InventoryTransaction`, `WarehouseStock` mutation logic, or stock-balance query was reimplemented in `Clovent.Restaurant` - every one of those already exists in `Clovent.Inventory.Application` and is reached only through `IMediator`, exactly as `InventoryArchitecture.md` Section 1 describes Inventory itself depending on Catalog.

---

## 7. Application layer: `Clovent.Restaurant.Application`

Same shape as every prior Application project - MediatR commands/queries/handlers/DTOs, `AddApplication()` scanning this assembly.

| Aggregate | Create | Mutations | Notes |
|---|---|---|---|
| DiningArea | ✓ | Rename, Activate, Deactivate | Branch-scoped, mirrors `Warehouse` |
| Table | ✓ | SetCapacity, Occupy, Vacate, Reserve, SetOutOfService, ReturnToService, Activate, Deactivate | Dual status: `RestaurantStatus` (lifecycle) vs `TableOccupancyStatus` (floor state) - independent concepts |
| Order | ✓ | Hold, Resume, Void, Cancel, Reopen, SetNotes, SetCustomerNotes, TransferTable, Complete, MergeTables, SplitOrder | No Edit - notes/table/status each have their own dedicated command |
| OrderLine | ✓ | SetQuantity, SetNotes, Void, Unvoid, TransferToOrder (internal, used by Merge/Split), Remove | Snapshots price/tax at creation (Section 3) |
| Discount | ✓ | *(none - immutable once created)* | Applied/Removed from an order via `Order`'s own commands |
| ServiceCharge | ✓ | *(none - immutable once created)* | Same shape as Discount |
| Payment | ✓ | Void (one-way) | Multiple payments per order is how partial payment/split bill work (Section 4) |
| PaymentMethod | ✓ | Rename, Activate, Deactivate | Mirrors `Brand` |
| KitchenTicket | ✓ (via SendOrderToKitchen) | Start, MarkReady, Serve, Cancel | See `KitchenWorkflow.md` |

**Flat "list everything" queries were added where the Desktop layer needed to scope one level deeper than the domain's own hierarchy** - `ListAllTablesQuery`/`ListAllDiningAreasQuery` (a table's floor-plan picker shouldn't force a cashier through Organization → Company → Branch → Dining Area first) and `GetOpenOrHeldOrderByTableQuery` (resolving "what order is currently at this table" for the POS screen's table picker) - the same additive-query pattern `MasterData.md` Section 7 and `WarehouseManagement.md` Section 3 already document for `ListAllWarehousesQuery`.

---

## 8. Infrastructure: `RestaurantDbContext`, `Restaurant` schema

Nine `DbSet`s under the `Restaurant` schema. Decimal columns use `.HasPrecision(18, 4)` (`.HasPrecision(5, 2)` for `OrderLine.TaxRatePercentage`), the established convention. `Order`'s four id-lists and `KitchenTicket.OrderLineIds` use the same JSON-column `ValueConverter`+`ValueComparer` pattern `Organization.CompanyIds`/`Authorization.md` Section 2's `RoleIds`/`PermissionIds` already established - reconstruction goes through the aggregate's constructor, and the `ValueComparer`'s snapshot function clones via `.ToList()` rather than returning the live reference, the same fix `Authorization.md` Section 2 documents catching a real bug the first time this pattern was needed.

**The `InitialCreate` migration succeeded on the first attempt** - no SQLite-vs-SQL-Server discovery this time (`AuthenticationInfrastructure.md` Section 11, `OrganizationArchitecture.md` Section 4, `InventoryArchitecture.md` Section 5 each document one), because every lesson from those three was applied proactively: JSON-column converters from the start, no `OrderBy` over a raw `DateTimeOffset` column anywhere in the new repositories.

Repositories and `IUnitOfWork`/`UnitOfWorkBehavior` mirror every prior project's shape exactly.

---

## 9. Desktop: ten screens/dialogs, mostly on existing shared infrastructure

Dining Area and Table Management reuse `MasterDataListView<TDto>`/`OrganizationHierarchySelector`/`EntityPicker` unchanged, the same infrastructure `WarehouseManagement.md` documents. Table Management's five extra floor-state actions (Occupy/Vacate/Reserve/Out of Service/Return to Service) use `MasterDataListAction<TDto>`'s `IsEnabledFor` predicate to gate each button on the row's current `OccupancyStatus` - no new list-view capability was needed.

**`EntityPicker` gained one additive method: `SelectId(Guid?)`.** Every existing screen's `LoadItems` call always selects the first item, correct for a picker that only ever scopes a static list. The POS screen's Table picker reloads its options after every state change (an order created, a table transferred) but needs the user's *current* selection preserved across that reload, not reset to whatever table happens to sort first - `SelectId` restores a specific selection without touching `LoadItems`'s existing behavior, so every prior `EntityPicker` consumer (Variant/WarehouseStock/StockAdjustment/StockTransfer/InventoryTransactions scoping, `WarehouseManagement.md` Section 3) is unaffected.

**Two small shared dialogs, `Clovent.Desktop.Restaurant.Shared`**: `TextPromptForm` (single free-text field, optional-required) backs every notes/reason prompt (Order Notes, Customer Notes, Item Notes, Void/Cancel reasons); `SelectionPromptForm` (single combo) backs "which existing record to act on" prompts (Remove Discount, Remove Service Charge). Both are `MasterDataEditFormBase` subclasses, the same dialog-shell reuse every prior create/edit form in this solution already follows.

**`RestaurantPosView` is the one Desktop screen with no prior template to build on** - every earlier milestone's screens are single-entity CRUD grids; a POS screen is a header-plus-lines transactional workflow with no precedent in this codebase. It composes a Table/Warehouse picker, a product-add panel (barcode entry falling back to SKU match, plus a browsable variant grid), an order-lines grid, and a right-hand action rail (all lifecycle/discount/service-charge/table operations) around one `OrderDto?` piece of state, refreshed as a whole after every action - no partial/optimistic UI updates. `PaymentForm` is a second new pattern: unlike every `MasterDataEditFormBase` dialog (single OK/Cancel round trip), it performs its own `IMediator` calls directly and re-queries the running balance after each one, since a payment screen is inherently stateful, not a single data-entry form. `ReceiptFormatter`/`ReceiptPreviewForm` render a plain-text receipt preview from the same `OrderTotalsCalculator` figures the POS screen shows, so the two can never disagree.

**Running Orders/Hold Orders/Kitchen Ticket Viewer are read-and-act overview screens**, not a second place to edit an order - each is a thin `MasterDataListView<TDto>` with lifecycle actions only (Hold/Void/Cancel/Send to Kitchen; Resume/Cancel; Start/Mark Ready/Serve/Cancel respectively), following the exact list+action-buttons shape `WarehouseManagement.md` Section 2 already established for `WarehouseStock`'s Receive/Issue/Reserve/Release.

**Dashboard gained five widgets**: Today's Sales, Open Tables, Running Orders, Kitchen Queue, Top Selling Items. `RestaurantDashboardCalculations` (`Clovent.Desktop.Dashboard`) extracts the pure logic (occupied-table counting, filtering completed-today orders, ranking top-selling variants by quantity) the same way `CatalogDashboardCalculations` does for the Inventory widgets (`Dashboard.md` Section 6) - Today's Sales and Top Selling Items both walk every completed order's totals/lines individually, the identical "fine at demo scale, not meant to scale to thousands" honest simplification `Dashboard.md` Section 6 already flags for Inventory Value.

---

## 10. Verified: builds clean, tests pass

- `Clovent.Restaurant` (Domain): 99 tests.
- `Clovent.Restaurant.Application`: 72 tests.
- `Clovent.Restaurant.Infrastructure`: 35 tests (33 SQLite-backed repository/`UnitOfWorkBehavior` tests, plus 2 new full-order-lifecycle integration tests spanning Restaurant/Catalog/Inventory in one real `MediatR` pipeline - see Section 6's inventory-deduction path exercised end to end).
- `Clovent.Desktop`: 0 build warnings, 0 errors, all ten screens/dialogs plus the shared-infrastructure extensions and five Dashboard widgets.
- `Clovent.Desktop.Tests`: 77 tests (up from 73 pre-Milestone-15 - the delta is `RestaurantDashboardCalculationsTests`).
- 0 build warnings, 0 errors across every Restaurant-related project.

---

## 11. Open questions for Solution Architect review

1. **The "no `WarehouseStock` record → skip the issue silently" rule (Section 6)** is a usability judgment call, not a specified requirement. **Needs a decision**: should a made-to-order/untracked line instead require an explicit per-product "untracked" flag on the Catalog side, rather than an implicit "no stock record exists" inference at completion time?
2. **`Today's Sales`/`Top Selling Items` walk every completed order individually** (Section 9), the same per-item-query pattern already flagged in `Dashboard.md` Section 6/`WarehouseManagement.md` open question #3 for Inventory Value - now a third instance of the same concern. **Needs a decision**: does a future milestone warrant a dedicated sales-reporting read model instead of composing existing per-order queries on every Dashboard load?
3. **`RestaurantPosView`'s feature-authorization codes are all namespaced under one `pos.*` feature** (Section 9/`Authorization.md`'s `feature.{code}` convention) rather than one code per fine-grained action having its own top-level feature name - a deliberate scope call to keep the permission list manageable, but worth ratifying alongside `Authorization.md`'s still-open item 4 on `feature.{code}` conventions in general.

---

## 12. Desktop UI Rebuild Phase 1: `RestaurantPosView` rebuilt as a purpose-built POS screen

Phase 1 of the Desktop UI rebuild (see `DesktopBootstrap.md` Section 8 for the Shell/Ribbon half of this phase) replaced `RestaurantPosView`'s Section 9 layout - a generic entity-CRUD shape (a product *grid* on the left, a flat seventeen-button column on the right) - with a purpose-built selling screen. No Application/Domain/Infrastructure code changed; every `IMediator` command/query call, every feature-authorization check, and `PaymentForm`/`TableTransferDialog`/`MergeTablesDialog`/`BillSplitDialog`/`DiscountDialog`/`ServiceChargeDialog`/`TextPromptForm`/`SelectionPromptForm`/`QuantityPromptForm` are all reused exactly as Section 9 already documents - this section covers presentation only.

**Layout, top to bottom/left to right:**
- **Top context bar** (full width): screen title, cashier name (`ICurrentSession.DisplayName`), the Warehouse/Table `EntityPicker`s, prominent accent-colored **NEW DINE-IN**/**NEW TAKE AWAY** buttons, and the current order's status badge (`{OrderNumber} • {OrderType} • {Status}`).
- **Left: product discovery** - a category button strip (from `ListProductCategoriesQuery`, unchanged), a search box, and a wrapping panel of tappable product tile cards (name/SKU/selling price) in place of the old read-only grid, plus the barcode-scan row at the bottom.
- **Center: the running order** - the order-lines grid (unchanged columns/behavior) with its compact edit-qty/notes/void/remove/refresh toolbar directly beneath it.
- **Right: grouped action rail** - the same lifecycle/table/adjustment/notes actions Section 9 already lists, now organized into four headed, two-column groups (Order; Table; Adjustments; Notes) inside a scrollable panel, instead of one flat seventeen-item column.
- **Bottom: totals + Pay** (full width) - Subtotal/Tax/Discount/Service Charge/Paid/Balance in a row, Grand Total set apart in a larger bold font, and a visually dominant accent-colored **PAY** button on the right, always in view regardless of how tall the center/right content grows.

**One new read**, added purely for the product tiles' price display: each active variant's current Selling price is looked up via `ListProductPricesByVariantQuery` at load time (mirroring `AddOrderLineCommandHandler`'s own `PriceType == "Selling"` resolution, and the identical per-item-query "fine at demo scale" pattern Section 9/10 already applies to Inventory Value and Today's Sales) - display-only, `AddOrderLineCommand` still resolves and snapshots the authoritative price server-side exactly as before.

**Docking order matters and is deliberate**: controls are added `centerPanel` (Fill) → `rightPanel` (Right) → `leftPanel` (Left) → `topBar` (Top) → `totalsBar` (Bottom), so the top and bottom bars span the full window width and the left/right panels fill the height between them, with the order grid filling whatever remains in the center - the same "Fill added first, then outer edges" convention `ShellForm.Designer.cs` uses for its own Ribbon/status-bar/workspace layout.

**Re-navigating to `pos` while an order is displayed no longer silently discards it** - `ShellForm.NavigationButtonItem_ItemClick` (`DesktopBootstrap.md` Section 8) skips re-navigation to the already-current key, since every workspace view is Transient and a fresh `RestaurantPosView` would otherwise replace the current one (and its in-memory `_currentOrder`) with a blank screen.

**Split into `.cs`/`.Designer.cs`**, same convention as `ShellForm`/`DashboardView`/`LoginForm` (`DesktopBootstrap.md` Section 9). Every event subscription that was previously an inline lambda (`_holdButton.Click += async (_, _) => ...`) is now a named handler (`HoldButton_Click`, `ProductTile_Click`, etc.) wired in the Designer partial's `WireEvents()` - see `DesktopBootstrap.md` Section 10 for the full event inventory.

**What did not change**: every `pos.*` feature-authorization check and `UpdateButtonStates()`'s enable/disable rules (Section 9); the order-lines grid's columns/behavior; every dialog this screen launches; the Table/Warehouse `EntityPicker` reload/`SelectId` pattern (this section's own earlier paragraph). Payment, Receipt, Dining Areas/Tables, Running/Held Orders, and Kitchen screens are unchanged - out of scope for Phase 1 (`DesktopBootstrap.md` Section 8's roadmap).
4. **No screen exists yet to manage `Discount`/`ServiceCharge` as standalone reference data** (e.g. a fixed list of standard discount reasons/percentages a cashier picks from) - every one is entered free-form via `DiscountDialog`/`ServiceChargeDialog` at apply-time. Acceptable for this milestone's scope; flagged in case a future requirement wants a managed catalog of standard discounts/charges.

---

## 13. Restaurant UX Refinement: Menu Items as a pure presentation layer over Catalog

A follow-up pass, explicitly scoped to **not** touch `Clovent.Restaurant`'s Domain/Application/Infrastructure, `Clovent.Catalog`'s Domain/Application/Infrastructure, or the database - Catalog's `Product`/`ProductVariant`/`ProductPrice` aggregates, their repositories, and every CQRS handler already documented in `CatalogArchitecture.md` and Section 7/8 above are completely unchanged. The goal was purely experiential: a Restaurant owner should never feel like they're driving a generic ERP Product module, and should never need to open Catalog's own screens for day-to-day menu/category upkeep.

### 13.1 `MenuItemsForm`: a new Desktop screen, zero new persistence

`src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemsForm.cs` (+ `.Designer.cs`) is the Restaurant-facing replacement for ever needing to open Catalog's `ProductsForm`. **There is no `MenuItem` entity, table, repository, or handler anywhere in this solution.** Every row on this screen is a Catalog `ProductVariant` - the identical aggregate `RestaurantPosView`'s product tiles already read (`ListProductVariantsQuery`, unchanged) - joined at load time (in the Desktop layer, not a new query) with its owning `Product`'s category name and its current active Selling `ProductPrice` (`ListProductPricesByVariantQuery`, unchanged), the same per-item "fine at demo scale" join pattern Section 9/12 already use for POS tiles and the End-of-Day/Sales Summary report. The grid shows only **Name, Category, Selling Price, Status** - no SKU column, no Variant/Price List vocabulary anywhere in the UI.

**One new Application-layer piece, added in the prior pass and reused unchanged here: `CreateProductWithPriceCommand`** (`Clovent.Catalog.Application.Products.Commands`). "New Menu Item" is the only place Restaurant creates a sellable item, and it is a single call to this one existing command - a Product, a default Variant, and one active Selling Price, in one transaction. SKU is generated automatically from the name; Unit of Measure and Currency are defaulted to whichever one is currently configured (an administrator sets these up once via Catalog's own screens, same as they already must for every other module) rather than ever being asked of a Restaurant owner. Editing reuses five already-existing granular commands exactly as `ProductsForm.EditAsync` already composes multiple commands per save: `RenameProductCommand` + `RenameProductVariantCommand` (kept in sync - the variant's own name is what POS actually displays) + `SetProductCategoryCommand` + `UpdateProductPriceAmountCommand` (or `CreateProductPriceCommand` in the edge case where no active Selling price exists yet) + `Activate|DeactivateProductVariantCommand` (only sent when the Active toggle actually changed, avoiding the domain's own "already active" guard exception).

**Activate/Deactivate targets the Variant, not the Product.** `RestaurantPosView.LoadAsync` has always filtered tiles on `ProductVariant.Status`, not `Product.Status` (Section 12) - so this screen's Active toggle and quick Activate/Deactivate buttons call `Activate|DeactivateProductVariantCommand` only. Deactivating the Product too would be over-reaching: a Product with more than one Variant (an advanced Catalog setup a Restaurant owner doesn't know exists) would have every variant pulled from sale, not just the one being edited here.

**"New Category" is a quick-add button on this screen**, not a separate visit to Catalog's own Categories screen: it opens the same `TextPromptForm` every other single-field Restaurant POS prompt already uses (Order Notes, Void Reason, etc.) and sends the existing `CreateProductCategoryCommand` (`Clovent.Catalog.Application.Categories.Commands`, unchanged). This is the other half of "never need to visit Catalog" - a Restaurant owner can define both menu items and their categories without ever opening the Catalog Ribbon page.

### 13.2 Optional item photo: a file convention, deliberately not a database column

The brief asked for an optional photo per menu item. Catalog's `Product` aggregate has no image field, and adding one would be a schema change to a table every other module (Retail, Purchasing, Manufacturing, whenever they arrive) inherits for a feature only Restaurant asked for - exactly the kind of duplication/scope-creep this refinement was explicitly told to avoid. `MenuItemImageStore` (`Clovent.Desktop.Forms.Restaurant.MenuItems`, internal to the Desktop project) instead stores an optional PNG file per `ProductId` under the current user's local application data folder (`%LocalAppData%\Clovent\MenuItemImages\{productId:N}.png`) - no migration, no new column, and nothing outside the Desktop project needs to know this convention exists. `MenuItemEditForm`'s photo picker (a `PictureEdit` + Choose/Clear buttons) works purely with an in-memory `Image` the caller (`MenuItemsForm`) then hands to `MenuItemImageStore.Save`/`Delete` after the Product/Variant/Price commands succeed.

### 13.3 POS: larger tiles, larger category buttons, and the same optional photo

`RestaurantPosView`'s product tiles grew from 130x90 to 168x(108 or 176, whichever fits the photo), the category button strip's buttons grew from a plain 26px-tall `SimpleButton` to a bold 40px-minimum one, and the tile's SKU label was removed entirely (Restaurant users never see it, matching 13.1's grid). Where a menu item has a photo, its tile now shows it - looked up once per session via `MenuItemImageStore` alongside the existing per-variant selling-price lookup in `LoadAsync` (the same "load once, not per-keystroke" reasoning `_sellingPricesByVariantId` already established, since `ApplyProductFilter`/`RenderProductTiles` rebuild every tile on every search keystroke). No `IMediator` call, command, query, or feature-authorization check in `RestaurantPosView` changed - this section is presentation-only, the same scope boundary Section 12 already drew for the original POS rebuild.

### 13.4 Sales Summary: the same End-of-Day report, renamed and re-cast as stat cards

The screen at Restaurant → Closing (`EndOfDayReportView`, `endofday` navigation key and feature code, both left unchanged - renaming either would ripple through every seeded permission and registration for zero user-visible benefit) is now captioned **Sales Summary** on screen and in the Ribbon. `GetEndOfDayReportQueryHandler` gained one new figure, `CardCollected` - the exact same kind of fragile-but-documented substring match (`"card"`, case-insensitive) Section 9/12 already accepts for `CashCollected`'s exact `"Cash"` match, since `PaymentMethod` still has no typed Cash/Card distinction. The Summary tab's five stacked labels became four bordered stat cards - **Total Bills** (`ReceiptCount`), **Total Sales**, **Cash**, **Card** - with Voided Orders/Average Sale kept as smaller secondary text beneath, and the "Items Sold / Top Selling" tab was recaptioned **Top Selling Items**. Every other tab (Cash Summary, Bills, Inventory Movement, Stock Remaining), the Today/Yesterday/date-range filters, and every Preview/Print/Export PDF/Export Excel action from the prior pass are unchanged.

### 13.5 Navigation: one new key, one caption change, no removals

`("menuitems", "Restaurant", "Menu", "Menu Items")` was added to `MainForm.Designer.cs`'s `NavigationItems` table (ahead of `pos`, so the Ribbon reads Menu → POS → Dining → Orders → Kitchen → Closing, top to bottom in the order a shift actually uses them), registered in `Program.cs`/`DesktopServiceCollectionExtensions.cs` the same way every other Milestone 13-15 screen already is, and given its own feature codes (`menuitems.{create|edit|activate|deactivate|createcategory}`) seeded alongside every other screen's in `DevelopmentAuthorizationSeedStartupTask`. `endofday`'s row had its Caption changed to "Sales Summary" (Key/Group unchanged). **Catalog's own `products`/`categories`/`variants`/`prices` navigation entries were deliberately left in place** - Catalog remains the single source of truth for every module, present and future, and an administrator setting up units of measure/currencies/advanced multi-variant products still needs them; what changed is that a Restaurant owner's day-to-day menu/category work no longer requires opening them.

### 13.6 Verified

- `Clovent.Catalog.Application`, `Clovent.Restaurant.Application`, `Clovent.Desktop`: 0 build warnings, 0 errors.
- `Clovent.Catalog.Application.Tests`: 39 tests (unchanged from the prior pass - `CreateProductWithPriceCommand` was reused, not modified).
- `Clovent.Restaurant.Application.Tests`: 79 tests (2 new: `CardCollected` split correctly between a "Credit Card"/"Debit Card"-named method and a same-day "Cash" payment).
- `Clovent.Desktop.Tests`: 79 tests (unaffected - this pass touched no pure-logic class this suite covers).
- **No interactive WinForms designer/display was available to visually verify the new screen or the POS/Sales Summary layout changes** - the same caveat every prior Desktop milestone in this document records (Section 8.6/`DesktopBootstrap.md` Section 8.6); everything above this line is build/test-verified only.

---

## 14. Commercial POS Polish Pass: Current Bill layout, currency display, colored badges

A further UI-only pass, explicitly scoped exactly like Section 13 - no Domain/Application/Infrastructure/Database change anywhere in `Clovent.Restaurant`, `Clovent.Catalog`, or `Clovent.MasterData`. The brief this time was narrower and more visual: make the Restaurant module read as commercial POS software a client can be shown, not an internal tool - a photo/price/status grid a restaurant owner recognizes, a POS screen laid out the way Toast/Square/point-of-sale terminals are (category rail / tile wall / running bill), and money that reads as money instead of a bare decimal.

### 14.1 Two small shared helpers, reused everywhere money/status appears

**`Clovent.Desktop.Forms.Base.CurrencyDisplay`** is a static `Configure(symbol, decimalPlaces)`/`Format(amount)` pair. Every screen that already resolved "the first configured `Currency`" for its own writes (`MenuItemsForm.CreateAsync`, `RestaurantPosView`'s `CreateProductWithPriceCommand` call - Section 13.1) now also calls `CurrencyDisplay.Configure` with that same currency once per load/refresh, so every label and grid column it formats agrees with what a save would actually use. A static holder (not per-screen state) is deliberate: this is a single-user desktop process with exactly one configured currency at a time - the identical "fine at this scale" reasoning Section 9/12/13 already apply to per-item price/image lookups. `MenuItemsForm`, `RestaurantPosView`, `EndOfDayReportView`, `ReceiptFormatter`, and `PaymentForm` all format money through it now instead of five different ad hoc `:N2` call sites.

**`Clovent.Desktop.Forms.Base.StatusBadgeStyler.Apply(view, column, isPositive)`** wires a `GridView.RowCellStyle` handler that tints a text column's cells green/red (with bold, centered text) based on a predicate over the cell's own string value - the standard DevExpress way to recolor a cell without a custom-draw handler. `MenuItemsForm`'s Status column is the first (and, for now, only) consumer; the helper takes a `GridView`/`GridColumn`/predicate rather than being Menu-Items-specific so any future grid with an Active/Inactive-shaped column can reuse it without copying the coloring logic.

### 14.2 Menu Items grid: Photo, currency, badge, "Menu Item" instead of "Name"

`MenuItemsForm`'s grid gained a `Photo` column - a `RepositoryItemPictureEdit` bound to a per-row `Image?` sourced from `MenuItemImageStore.Load` (Section 13.2's existing file-convention store, still zero new persistence), with `GridView.OptionsView.RowAutoHeight = false`/`RowHeight = 56` so every row renders the same fixed-size thumbnail regardless of whether that particular item has a photo - an auto-height row would otherwise visibly shrink for photo-less items, making the grid look uneven. Loaded images are cached in a `Dictionary<Guid, Image>` and disposed on the next `RefreshAsync`/on form close, the same lifecycle `RestaurantPosView.LoadAsync`'s `_tileImagesByProductId` already established. "Name" became "Menu Item" (caption only, `FieldName` unchanged), Selling Price now displays via `CurrencyDisplay.Format` (a `GridView.CustomColumnDisplayText` handler keyed on the column reference), and Status renders as a colored badge via `StatusBadgeStyler`.

### 14.3 Menu Item dialog: "Save & New", and an opt-in extension to the shared dialog shell

`MasterDataEditFormBase` (shared by every `*EditForm` in the solution, not just Restaurant's) gained one opt-in method, `EnableSaveAndNew(saveCaption)`, and one new protected `SavedAndNew` flag - both are no-ops for every dialog that doesn't call the method, so `ProductEditForm`/`UserEditForm`/`RoleEditForm`/every other existing edit dialog is byte-for-byte unaffected. `MenuItemEditForm` is the first (and only) caller: it relabels the base OK button "Save", adds a "Save & New" button beside it, and exposes `IsSaveAndNew` publicly so `MenuItemsForm.CreateAsync` can loop - save, then immediately reopen a fresh dialog for the next item - instead of forcing a restaurant owner keying in a whole menu section (every Curry, every Drink) back through the command panel's "New Menu Item" button after each one. The dialog also gained a centered bold "Menu Item" heading above its fields (a `LabelControl` added after `EnableSaveAndNew`, docked Top); `MasterDataEditFormBase`'s own `Load` handler that recomputes the dialog's minimum client size (Section 8's `ProductEditForm` clipping fix) was generalized to sum in *any* extra Top/Bottom-docked chrome a subclass adds beyond its own button panel, not just the content `TableLayoutPanel` - otherwise the new heading's height would have silently reproduced the exact clipping bug that Load handler was written to prevent.

### 14.4 `RestaurantPosView`: Left/Center/Right rebuilt to match a commercial POS's own layout

Phase 12's layout (product discovery combining categories+tiles on the left, the order grid centered, all seventeen order/table/adjustment/notes actions in one flat right-hand rail) is replaced by the three-pane shape a restaurant owner's own reference mockup showed: a **left category rail** (large, full-width, one-tap buttons, now vertically stacked rather than wrapped horizontally), a **center product tile wall** (search box, tiles, barcode entry - unchanged internally, only the surrounding panel changed), and a right **"Current Bill"** panel. No `IMediator` call, command, query, or `pos.*` feature-authorization check changed anywhere in this pass - every handler body in `RestaurantPosView.cs` is untouched except for currency formatting and the new Print Bill action; this section is presentation-only, the identical scope boundary Sections 12/13 already drew.

**The Current Bill panel** holds the order-lines grid - now exactly four columns (Qty/Item/Price/Total, matching the brief's own mockup) instead of seven (Sku/Notes/Voided are still read from the bound `OrderLineRow` by the edit/void/notes handlers, just never shown as their own column); a voided line is shown greyed-out and struck-through via `RowCellStyle` rather than needing a separate "Voided" column to say so. Beneath the grid: Subtotal/Discount/Tax/Grand Total in that order (the brief's own order, Grand Total set in a much larger bold font), then Paid/Balance as smaller secondary text relevant only mid-payment. Below the totals, two rows of equally-sized buttons - **Hold / Clear / Print Bill**, then **Send to Kitchen / Complete Order / More Actions** - above one full-width, dominant **PAY** button, the single most visually prominent control on the screen exactly as Section 12 already established. "Clear" is `_cancelOrderButton` relabeled, not a new code path - a cashier abandoning a not-yet-paid ticket is exactly `CancelOrderCommand`, reason prompt included. **Print Bill** is the one genuinely new action: `ReceiptFormatter.FormatAsync` + `ReceiptPreviewForm`, the identical pair `PaymentForm.ShowReceiptAsync` already uses, now also reachable directly from the POS screen without opening Payment first.

**"More Actions"** is a `ContextMenuStrip` (`_moreActionsButton`, top-right of the button grid) holding the twelve actions a cashier needs only occasionally - Resume/Void Order/Reopen (order-lifecycle edge cases), Transfer Table/Merge Tables/Split Bill, Order Notes/Customer Notes, Add/Remove Discount, Add/Remove Service Charge - grouped by `ToolStripSeparator`s in the same four groupings Section 12's action rail used. Each is a `ToolStripMenuItem`, not a `SimpleButton` - both expose `.Enabled` and a compatible `Click` `EventHandler`, so every existing handler method and every line of `UpdateButtonStates()`'s enablement logic in `RestaurantPosView.cs` needed zero changes beyond the field type declaration in the Designer partial.

**The order-status text became a colored badge** (`_orderStatusLabel.Appearance.BackColor`/`ForeColor`, set by the new `UpdateOrderStatusBadge()`): green while Open, amber while Held, red once Voided/Cancelled, blue while Completed - the same visual language `StatusBadgeStyler` gives Menu Items' Active/Inactive column, hand-rolled here rather than reused verbatim since it needs four colors (not two) and lives on a `LabelControl`, not a grid cell. Product tile prices and the line grid's Price/Total columns now format through `CurrencyDisplay` instead of bare `:N2`.

### 14.5 Sales Summary: currency formatting only

`EndOfDayReportView` needed no layout change - Section 13.4's KPI cards/Bills grid/Top Selling Items tab already matched the brief. Every money figure (the four stat cards, every grid's Total/Amount column via a `CustomColumnDisplayText` handler keyed on field name, the printed summary text) now formats through `CurrencyDisplay` instead of bare `:N2`, so a restaurant owner sees "Rs.850.00"-shaped figures (whatever currency is actually configured) everywhere, not just on the POS screen.

### 14.6 Verified

- `Clovent.Desktop`: 0 build warnings, 0 errors (`dotnet build` on the project and on the full solution).
- `Clovent.Desktop.Tests`: 79 tests, all passing (unaffected - this pass touched no pure-logic class this suite covers).
- `Clovent.Restaurant.Application.Tests`: 79 tests, all passing (unaffected - `ReceiptFormatter`'s only change was money formatting, not figures).
- **No interactive WinForms designer/display was available to visually verify the new Menu Items grid, Menu Item dialog, or POS Current Bill layout** - the same caveat Sections 8.6/12/13 already record; everything above this line is build/test-verified only.

---

## 15. Demo Readiness Pass

A follow-up pass with a narrower goal than Section 14's: not "does it look like commercial POS software" but "can a restaurant owner who has never seen this screen before follow it, end to end, without getting stuck or confused." Traced by reading the actual code path a first-time owner would hit - add a menu item, sell it in POS, print the bill, collect payment, complete the order, and confirm it shows up in Sales Summary - rather than reviewing each screen in isolation. No Domain/Application/Infrastructure/Database change; every fix below is Desktop-only.

### 15.1 Terminology leaks fixed: "Warehouse" -> "Location", SKU removed from anything customer- or owner-facing

Both POS and Sales Summary literally labelled their scope picker **"Warehouse:"** and showed message boxes reading "Select a warehouse first."/"No Warehouse Selected" - the exact `Clovent.MasterData` concept Section 13's whole premise was to hide from a restaurant owner, just missed in the first UI pass because it's a cross-cutting picker, not a Catalog field. Both are now captioned **"Location:"** with matching message text; the underlying `WarehouseId`/`ListAllWarehousesQuery` are unchanged. Since `EntityPicker.LoadItems` already auto-selects the only item when there's just one, a single-location restaurant (the common case) now never sees the picker at all - it's hidden (`Visible = warehouses.Count > 1`) rather than shown-but-pointless.

**Three more SKU leaks, found by reading what a customer actually receives**: `ReceiptFormatter.ResolveVariantNameAsync` was prefixing every printed receipt line with the item's SKU (`"{variant.Sku} {variant.Name}"`) - meaning the literal paper handed to a customer read like an ERP pick-list, not a restaurant bill. `RestaurantPosView.SplitBillAsync`'s line picker had the same prefix. Both now print/show just the item name. Sales Summary's Top Selling Items/Inventory Movement/Stock Remaining grids each had their own SKU column, removed for the same reason (widening "Product" to "Menu Item" to fill the freed space); `EndOfDayReportView`'s "Completed (UTC)"/"Occurred (UTC)" timestamp columns are recaptioned "Completed"/"Occurred" and now display in the viewer's local time instead of raw UTC - a restaurant owner reading a bill list shouldn't have to mentally convert time zones.

### 15.2 Menu Items: empty states, stronger validation, a real photo preview, double-click to edit

An empty grid (first launch, or a search/category with no matches) previously just showed blank column headers. It now shows a centered message - "No menu items yet. Click '+ New Menu Item'..." the first time, "No menu items match your search." once items exist but the filter excludes all of them - reusing the same overlay-label-over-grid technique for both Menu Items and POS's product tile wall. `MenuItemEditForm` gained a real photo preview: the picture box shows a bordered "No Photo" placeholder until one is chosen (previously a blank white square, indistinguishable from a broken image), fields are now labelled "Item Name:"/"Photo:" instead of "Name:"/"Image:", and validation messages are conversational ("Please enter a name for this menu item.", "Enter a selling price greater than 0." - a free/zero-priced item is far more likely a forgotten price than an intentional freebie). Deactivating an item (which pulls it off the POS tile wall immediately) now asks for confirmation naming the item; Activate does not, since it only ever adds something back. Double-clicking a grid row opens Edit directly. The primary "+ New Menu Item" button is bold to draw the eye, matching the "the owner should immediately understand how to add a menu item" goal directly.

### 15.3 POS: category selection is now visible, dead taps now say why, quantity bumps need one click

**Category buttons had no selected state at all** - after tapping "Curry", nothing on screen showed "Curry" was the active filter; a cashier interrupted mid-shift had no way to tell which category they were looking at. Whichever button (including "All Items") matches the current filter is now shown with an accent-colored background. **Tapping a product tile or scanning a barcode before starting an order silently did nothing** - confusing under time pressure, since a cashier can't tell a missed tap from a genuinely broken button. Both now show one specific sentence ("Start a New Dine-In or New Take Away order first.") instead of nothing. **Quantity changes** now have one-click **+/-** buttons next to the existing "Edit Qty" dialog - the common case ("make that two") no longer needs select-row -> open-dialog -> type -> OK for a single unit change. **"Clear"'s reason prompt** is now pre-filled with "Started by mistake" (still editable) so clearing an accidental empty order is one Enter/OK instead of composing a reason from scratch. The product tile wall gets the same empty-state treatment as the Menu Items grid, with a POS-specific hint ("...then come back here to sell them") when there are literally zero menu items configured yet.

### 15.4 The one real bug this pass found: completing an unpaid order raised a raw domain exception

Reading `CompleteOrderCommandHandler` (`Clovent.Restaurant.Application`) turned up a genuine, likely-to-happen failure mode: **"Complete Order" and "PAY" sit in the same button row** (Section 14.4), and clicking Complete before Pay throws `RestaurantDomainException.OrderNotFullyPaid` - whose message embeds the order's internal `OrderId` value object (`"Order 'OrderId { Value = ... }' still owes 500.00 and cannot be completed."`). Nothing upstream of `RestaurantPosView.CompleteAsync` caught this, so a cashier who completes before paying would have seen that raw internal identifier in the global `ErrorDialogForm` - exactly the unprofessional-appearance failure this whole initiative is meant to prevent, and a very plausible slip during a live demo. Fixed by checking the same `GetOrderSummaryQuery` balance `RefreshOrderAsync` already calls *before* sending `CompleteOrderCommand`: a nonzero balance now shows "This bill still has {amount} outstanding. Collect payment with PAY before completing the order." - one sentence, no internal identifiers, and it never reaches the domain layer's own exception. This is a UI-only guard; `CompleteOrderCommandHandler`'s own validation is untouched (still the authoritative check).

### 15.5 Sales Summary now shows Today by default

Opening this screen previously showed a "0.00 everywhere" dashboard until the owner clicked Today/Yesterday/Generate - not obvious to a first-time user that a click was even expected. It now auto-generates Today's figures on open (skipped silently, no dialog, if no location is configured yet - only an explicit Generate/Today/Yesterday click shows the "select a location" warning). `GenerateAsync`'s half-dozen sequential reads (report, every variant, every transaction, every stock line - the slowest single action in this screen) now show a wait cursor, matching the one `RestaurantPosView.LoadAsync` already had reason to show (per-variant price/photo resolution) - `EndOfDayReportView`/`RestaurantPosView` are plain `XtraUserControl`s, not `BaseForm` subclasses (Section 12's own note on `MenuItemsForm` being the one screen with a real busy overlay), so a full busy-overlay migration was out of scope for a UI-only pass; `UseWaitCursor` is the same affordance with none of that risk.

### 15.6 Verified demo workflow (traced by reading the code, not run interactively)

1. **Add a Menu Item** - `MenuItemsForm` -> "+ New Menu Item" -> fill Item Name/Category/Selling Price/Photo -> Save (or Save & New for a whole section at once). Requires at least one `UnitOfMeasure` and `Currency` already configured in Catalog/MasterData - both are seeded automatically by `DevelopmentCatalogSeedStartupTask`/`DevelopmentMasterDataSeedStartupTask` when `DesktopOptions.SeedDevelopmentRestaurantData`-equivalent flags are on, so a development-seeded environment needs no manual setup before this step.
2. **Open POS** - the new item appears as a tile (under "All Items" and its own category) once Active.
3. **Sell it** - NEW TAKE AWAY (simplest, no table) or NEW DINE-IN (pick a seeded table) -> tap the tile -> it appears in Current Bill.
4. **Collect payment** - PAY opens the Payment screen, amount pre-filled with the exact balance -> Record Payment.
5. **Complete the order** - Complete Order (now guarded per Section 15.4 if payment was skipped).
6. **Print the bill** - Print Bill (works at any point; printed after Complete, it reads as a paid receipt with the payment line included).
7. **Open Sales Summary** - now shows Today by default; the completed order is counted in Total Bills/Total Sales/Cash or Card (matched by payment method name - "Cash"/"Credit Card" are exactly what `DevelopmentRestaurantSeedStartupTask` seeds) and listed in the Bills tab.

**Step 5 (Complete Order) is load-bearing**: `GetEndOfDayReportQueryHandler` only counts `OrderStatus.Completed` orders (Section 6/Section 15.4) - an order that was sold and paid but never explicitly completed will not appear in Sales Summary at all. This is existing, unchanged domain behavior (Section 2/6), not a UI defect, but it is exactly the kind of step a scripted demo must not skip - see the Demo Script in the final report for the explicit walkthrough.

### 15.7 Known limitations (existing behavior, not changed by this pass)

- **First-time setup still needs an administrator to configure at least one Unit of Measure and Currency via Catalog's own screens** before "New Menu Item" will work (Section 13.1) - `MenuItemsForm` shows a clear "Ask an administrator to set up a unit of measure/currency first" message rather than failing silently, but there is still no in-app onboarding wizard for a genuinely blank installation with development seeding disabled.
- **Cash/Card KPI split is a fragile name-based match** ("Cash" exact, "card" substring - Section 6/9 of the underlying report query) - a `PaymentMethod` named anything else (e.g. "Mobile Wallet") is invisible to both figures, only showing up in the Cash Summary tab's per-method breakdown.
- **No undo for Remove Line/Void Line/Clear** beyond re-adding the item or reopening a cancelled order (Reopen, still available via More Actions) - deliberate, per the "reduce clicks" brief for POS actions taken constantly during a shift, unlike Menu Items' Deactivate confirmation.
- **Per-item, per-request price/photo/stock lookups** (Sections 9/12/13/15.5) remain the "fine at demo scale, not meant to scale to thousands of orders" simplification already flagged three times in this document - unchanged by this pass.
- **No interactive WinForms designer/display was available to visually verify any change in this pass** - everything in Section 15 is build/test-verified and code-traced, not screenshot-verified.

### 15.8 Future enhancements (explicitly not implemented - out of scope per this pass's brief)

Inventory depletion warnings in POS, recipes/modifiers, a Kitchen Display Screen, waiter/table-service roles, reservations, delivery/online ordering, a configurable tax engine, a discount rule engine, loyalty/rewards, cross-branch analytics, and any other capability outside Menu Items/POS/Sales Summary's existing scope were explicitly excluded from this pass and were not touched.

### 15.9 Verified

- `Clovent.Desktop`: 0 build warnings, 0 errors (`dotnet build` on the project and on the full solution).
- `Clovent.Desktop.Tests`: 79 tests, all passing.
- `Clovent.Restaurant.Application.Tests`: 79 tests, all passing (unaffected - no Application-layer code changed in this pass).

---

## 16. Layout Root-Cause Pass: the tender strip was never given its own reserved height

A follow-up pass triggered by continued reports that the POS/Login screens still rendered wrong at runtime despite Sections 12-15 each reporting a clean build - this pass deliberately looked for the structural reason the layout collapses instead of adjusting more pixel values, per the standing rule that a build passing is not evidence a screen renders correctly. No Domain/Application/Infrastructure change; both fixes below are Desktop-only, and both were root-caused by reading the actual `TableLayoutPanel` row/column wiring, not by trial-and-error resizing.

### 16.1 The payment tender strip: a declared-but-never-wired constant

`RestaurantPosView.Designer.cs` already declared `TenderStripHeight` with a comment claiming it "eliminates off-screen clipping" - but `_leftColumnLayout`'s row hosting `_paymentCardPanel` was wired as `RowStyle(SizeType.AutoSize)`, never `RowStyle(SizeType.Absolute, TenderStripHeight)`. The constant was never actually applied anywhere outside its own doc comment. Because `PaymentPanel` (the tender strip) is `Dock = DockStyle.Fill` inside that cell, and a Dock=Fill control reports no preferred size of its own to an AutoSize row, the row instead collapsed to whatever tiny design-time `Size` `PaymentPanel.Designer.cs` happened to declare (`1000x110`) - not enough for the payment-methods column (balance readout + header + a button row that realistically wraps to two rows at normal window widths), the 3x2 quick-cash grid, and a prominent Record Payment button to all render without visible crowding. This is the concrete mechanism behind "payment controls are not reliably visible" reported against every prior pass.

**Fix**: `_leftColumnLayout`'s second row is now `RowStyle(SizeType.Absolute, TenderStripHeight)`, and `TenderStripHeight` was recomputed from the strip's own tallest column rather than left at its previous unused value - 180px (recomputed with layout breathing room from: balance label ~26 + methods header ~18 + two 46px button rows ~92, plus the panel's own `Padding(8)` and the strip root's `Padding(2)` on each side, 20 total, with 24px buffer). `PaymentPanel.Designer.cs`'s own design-time `Size` was updated to `1000x180` to match, purely so opening that file's own Designer surface renders proportionate to the real runtime height.

### 16.2 LoginForm: a fixed 600px `ClientSize` shorter than its own content

`LoginForm`'s `tlpForm` centers its field group vertically using two `RowStyle(SizeType.Percent, 100F)` spacer rows (top and bottom) around fifteen `AutoSize` rows (title through "SELECT MODULE") and one `Absolute(130)` row (the module cards) - a sound technique, but only once the window is at least as tall as those sixteen rows' real, font/DPI-scaled height combined. The Designer-declared `ClientSize`/`MinimumSize` of `840x600` is smaller than that real content height on this environment's fonts, so `TableLayoutPanel` had nothing to give the two Percent spacer rows and compressed the `AutoSize` rows below their preferred height instead - the mechanism behind "Username area has previously been clipped" and PIN/label crowding reported against every prior pass.

**Fix**: rather than hand-picking a second hardcoded height (Section 18's own "no patch-and-guess" instruction), `LoginForm.LoginForm_Load` now calls `ApplyContentDrivenMinimumSize()`, which measures `tlpForm.GetPreferredSize(...)` (the real sum of its AutoSize/Absolute rows, ignoring the two zero-content Percent spacers) and grows - never shrinks - `ClientSize`/`MinimumSize` to fit, with 32px of breathing room. This is the same "measure real content, then size the chrome around it" technique `MasterDataEditFormBase`'s own `Load` handler already uses for edit-dialog minimum sizing (Section 14.3) - applied here to the sign-in window instead of a second static guess. `AutoScaleMode.Dpi`/`AutoScaleDimensions` were left untouched; this only changes what the *design-time* 96-DPI baseline height should have been, not how DPI scaling itself works.

### 16.3 Dead control removed: `_payBarTotalLabel`

`_totalsFooterLayout` carried an extra `_payBarTotalLabel` row that duplicated `_grandTotalLabel`'s figure but was permanently `Visible = false` with nothing in `RestaurantPosView.cs` ever toggling it - a vestigial control from an earlier layout iteration, not a second on-screen total. Removed (field, instantiation, grid placement, and its two `.Text =` assignments in `RestaurantPosView.cs`) to keep "Totals must have ONE clear logical location" (Section 14.4) true of the actual control tree, not just the visible one.

### 16.4 Two pre-existing build warnings fixed in passing

`CustomerEditForm.Designer.cs` declared an unused `components` field (CS0414) - `MasterDataEditFormBase` already owns disposal for every `*EditForm` subclass, so this was inert boilerplate; removed. `CustomerLedgerDialog.Designer.cs`'s `Dispose(bool)` override was missing its required XML doc comment (CS1591); added, matching every other Designer partial's identical `/// <summary>Clean up any resources being used.</summary>`.

### 16.5 What this pass did not (yet) verify

**No Windows GUI screen-capture/automation tool was available in this environment to launch the application and visually inspect the rendered Login or POS screens.** Every fix above is justified by tracing the actual `TableLayoutPanel`/`Dock`/`RowStyle` wiring against WinForms' own documented layout algorithm (AutoSize rows measuring contained controls' `GetPreferredSize`, Dock=Fill controls not contributing one, Percent rows only receiving space left over after Absolute/AutoSize rows are satisfied) and cross-checked against the concrete symptoms reported (clipped Username/PIN, crowded payment controls) - not by an interactive Designer session or a runtime screenshot. This is the same caveat Sections 8.6/12/13/14/15 already record for every prior Desktop-only pass in this document, repeated here rather than claimed away.

### 16.6 Verified

- `dotnet build "Clovent.BusinessOperatingSystem.slnx" -c Release`: 0 warnings, 0 errors (previously 2 pre-existing warnings, both fixed per Section 16.4).
- Full solution test suite run via `dotnet test` - see the final report for the pass/fail count captured for this pass.

---

## 17. RestaurantPosForm Completion Pass: Logout, searchable customer, tender keypad

A follow-up pass against the now-single-form `RestaurantPosForm` (Sections 12-16's `RestaurantPosView`/`PaymentPanel` split was superseded by an in-session single-form migration between passes - `RestaurantPosView.cs`/`PaymentPanel.cs`/`.Designer.cs` no longer exist; everything below targets `RestaurantPosForm.cs`/`.Designer.cs` directly). Unlike every prior pass in this document, this one had a real Windows GUI capability available: a `PrintWindow`/`CopyFromScreen`-based screenshot harness built via PowerShell/User32 this session, plus synthetic mouse/keyboard input, used to launch the actual built `Clovent.Desktop.exe`, log in, and drive the running POS screen - not source inspection alone. Every finding and fix below was reproduced against the real running application.

### 17.1 Logout

`RestaurantPosForm` had no way to end a session short of closing the window (or the whole process, for a POS-only sign-in, since `Program.cs`'s `Application.Run(posForm)` branch bypasses `Forms.Shell.MainForm` entirely). Added `_logoutButton` to the header's action flow, wired to `LogoutButton_Click`, which mirrors `MainForm.SignOutAsync`'s exact pattern (best-effort `RecordActivityCommand`, `_currentSession.SignOut()`, `Hide()`, a fresh `LoginForm` via `_scope.ServiceProvider.GetRequiredService<...>()` shown as an owned modal) rather than inventing a second session mechanism - extended with a three-way outcome switch on `loginForm.SelectedModuleKey` ("pos": reload this same window via `LoadAsync` for the new cashier; "backoffice": resolve and show `MainForm` since this standalone window has no Shell to hand off to; anything else: `Close()`) since, unlike `MainForm`, this window can legitimately need to hand off to a Shell that doesn't exist yet. Verified end-to-end on the running app: Logout signs out, shows Sign-in, re-authenticating as "pos" reloads the same window (customer/table/cart/totals/payment all correctly re-rendered) without mutating the order, payment, or customer balance.

### 17.2 Searchable customer selector

`_customerPicker` was an `EntityPicker` (a plain `ComboBoxEdit` with `TextEditStyle.DisableTextEditor` - deliberately not typeable, by the same control's own doc comment). Replaced with `DevExpress.XtraEditors.SearchLookUpEdit`, bound to a new `CustomerPickerRow(CustomerId, Name, Phone, BalanceDisplay)` projection of the existing `ListCustomersQuery` (Walk-in synthesized as `Guid.Empty` first, same sentinel `_currentOrder.CustomerId is null` already maps to everywhere else in this class) - no new query/search infrastructure. `SetSelectedCustomerId` centralizes every programmatic selection (guarding `_isRefreshingOrder` so reload/restore doesn't re-fire `SetOrderCustomerCommand`), replacing `EntityPicker.SelectId`'s four call sites.

**One real defect found and fixed during runtime testing**: `SearchLookUpEdit` rendered with no visible dropdown button at all - confirmed genuinely absent (not just low-contrast) via a zoomed screenshot, though F4 still opened the popup, proving the control itself worked and only the mouse affordance was missing. Fixed by explicitly adding `EditorButton(ButtonPredefines.Combo)` to `Properties.Buttons` (cleared first) - `SearchLookUpEdit` apparently does not auto-populate this the way other `LookUpEditBase` controls do. Re-verified: button now visible, mouse click opens the popup. Full round trip tested live: typed "walk" filtered the popup grid to just "Walk-in Customer" with the match highlighted; created a real customer ("John Smith") via the existing "+ New" → `CustomerEditForm` → `CreateCustomerCommand` path (hitting and satisfying the existing "Address is required" validation genuinely, not bypassed); the new customer was correctly auto-selected and the order's customer updated via the existing `SetOrderCustomerCommand` - no UI-side balance/customer mutation anywhere in this control.

### 17.3 Tender keypad

Added a 3-column x 5-row numeric keypad (`pnlKeypad`: 7/8/9, 4/5/6, 1/2/3, ./0/⌫, Clear spanning all columns) as a new column in `tlpPayment` (columns rebalanced 32/33/21/14 → 20/22/20/24/14 to fit it without growing the strip's existing height). Digit/decimal/backspace/clear handlers mutate `_amountEdit.Text` only - no new calculation path - since that field's existing `EditValueChanged` → `UpdateChangeDisplay()` wiring (already used by typing, Quick Cash, and Exact) recalculates Change for free. Verified live: Clear → "5" → "0" → "0" produced `537.5500` with Change `$0.05` (537.5500 − 537.50), matching the balance due exactly; Clear and Exact both re-verified working alongside the new keypad.

> **Correction (Section 18.1)**: the "produced `537.5500`" result above was the bug, not a passing verification - `KeypadDigitButton_Click` unconditionally appended (`_amountEdit.Text += button.Text`) onto whatever the field already displayed, including the balance-due prefill. It went unnoticed here because a fresh field already reads as "empty" to a casual glance even when it silently contains the prior balance text. Fixed in Section 18.1 below.

### 17.4 Responsive product tiles

Traced `_productTilesFlow` (`Dock=Fill`, `AutoScroll=true`, default `WrapContents=true`, fixed `160x150` tiles via `TileWidth`/`TileHeight`) - already structurally correct per Section 12's original design (Dock=Fill sizes the flow panel to the actual available width every layout pass; fixed-size tiles wrap by count, never stretch to fill the panel). Confirmed via screenshot at the form's enforced minimum size (`MinimumSize = new Size(1280, 720)` at the time — **now `1200x700`, see Section 21.3, defect D6**) that two tiles still render side by side rather than collapsing to one per row. Only two menu items exist in this environment's seed data, so wrapping to three-or-more columns at wider sizes could not be demonstrated with real data; the mechanism (Dock=Fill + fixed tile size + default wrap) is the same one already responsible for the two-column behavior observed, and needs no structural change.

> **Follow-up (Section 18)**: since real seed data still tops out at two menu items, `ProductTileWrappingTests.cs` (`Clovent.Desktop.Tests/Restaurant/Orders/`) now proves the 1/2/3/4-column claim directly - a `FlowLayoutPanel` built with the exact same `TileWidth`/`TileHeight`/`Margin`/`Padding`/default-`WrapContents` configuration is resized to the precise pixel width that fits N tiles and asserts exactly N land in the first row, for N in 1-4. This is a permanent regression test, not a one-off manual measurement.

### 17.5 One more defect found via the minimum-size resize test: "Exact" button text clipped to "xac"

Resizing to the form's enforced minimum (1280x720 DIP → ~3226x1813 physical at this environment's 250% DPI) showed the Amount/Change column's "Exact" button rendering as "xac" - confirmed by screenshot, not assumed. `_exactAmountButton` is `Dock=Fill` inside a 65/35 split with the amount textbox; `Dock=Fill` overrides `StyleProminentButton`'s `AutoSize=true`, so the button is forced to whatever width the 35% column leaves, and at this size that was too narrow for its own `Padding(14,4,14,4)` plus text. Fixed by rebalancing `_amountContainerLayout`'s columns to 58/42, giving the button enough width without touching the textbox's usability. ~~Re-verified at the same minimum size: "Exact" now renders in full.~~

> **Superseded 2026-08-13 — this fix was insufficient and the "renders in full" claim was wrong.**
> The independent QA pass measured `Exacı` at 1366x768 and `Exac` at the enforced minimum. Widening
> the column addressed the symptom's smaller half; the larger half was that `StyleProminentButton`
> also overrode the button's 8pt font with 9.5pt bold *and* applied `Padding(14,4,14,4)`, leaving
> under 40px for the caption regardless of column width. The helper call is now gone (see
> Section 21.3, defect D14) and the button keeps its own font, a 4px padding, and a `MinimumSize`
> floor. Current status: **PENDING CLAUDE QA**.

### 17.6 Verified

- `dotnet build "Clovent.BusinessOperatingSystem.slnx" -c Release`: 0 warnings, 0 errors.
- `dotnet test` (full solution): all tests passing. One `SerializedMediatorConcurrencyTests` failure was observed mid-pass while the app was under heavy concurrent screenshot/input load from this same verification session; re-ran isolated and it passed, and a subsequent full-suite run (after the final fix) was clean with no failures - logged here as flaky-under-load, not a regression, per Section 16 (same test, same conclusion).
- Runtime-verified on the built `Clovent.Desktop.exe` via screenshot + synthetic input: Login → POS → customer search/select/create → quantity change → keypad digit/decimal/backspace/clear/Exact → Logout → re-login → reload, all end to end, all against the real dev database (not design-time sample data - order `ORD-15`, customer "John Smith", persisted across process restarts).
- **Visual Studio's actual WinForms Designer was not opened** - Designer-compatibility here means "no DI/async/business logic added to `InitializeComponent`, only field/property assignments and object-initializer syntax already used elsewhere in this file," verified by successful compilation, not by an interactive Designer session.

## 18. RestaurantPosForm QA/fix pass: keypad correctness, logout DI-scope leak, 768px responsive layout

Two follow-up passes against `RestaurantPosForm`, both driven against the real running `Clovent.Desktop.exe` (login → POS, real dev database) rather than source inspection alone, using the same PowerShell/User32 screenshot-and-synthetic-input harness Section 17 established. The first pass re-audited Section 17's own claims instead of trusting them, found Section 17.3's "verified" keypad result was actually the bug it should have caught, and fixed it plus a real DI-scope leak in `LogoutButton_Click`. The second pass targeted a resolution Section 17 never actually tested - a real 1366x768/1280x768 POS display - and found and fixed one genuine clipping defect.

### 18.1 Numeric keypad: replace-on-first-digit instead of append-onto-prefill

`KeypadDigitButton_Click` did `_amountEdit.Text += button.Text` unconditionally. Since `LoadPaymentAsync`/`ExactAmountButton_Click`/`QuickCashButton_Click` all pre-fill `_amountEdit.Text` with a real value (the balance due, or the exact/quick-cash amount) rather than leaving it blank, pressing `5`, `0`, `0` against a $537.55 balance produced `537.55500` (Section 17.3 rounded this in its own prose to `537.5500`) instead of the `500` a cashier typing a tendered amount actually means.

Fixed by adding `_amountEntryIsPreset` (a field, not a UI element): true whenever `_amountEdit.Text` was just set programmatically to a value the cashier didn't type (initial balance-due bind, Exact, Quick Cash, Clear), false once a keypad digit has been pressed. `KeypadDigitButton_Click` replaces instead of appending exactly once, on the first digit after a preset value, then reverts to appending; `KeypadDecimalButton_Click` and `KeypadBackspaceButton_Click` treat a preset value the same way (decimal starts a fresh `"0."`; backspace clears the whole preset rather than deleting characters the cashier never typed). This is the same "typing over a suggested value replaces it" convention every mainstream POS/calculator keypad already uses - not a new interaction model.

Verified live at `Balance Due: $100.00`: Clear → `5` → `0` → `0` → `500`, Change `$400.00`; Clear → Exact → `100`; Clear → Quick Cash `500` → `500`, Change `$400.00`; pressing `5` directly on top of that preset `500` → `5` (not `5005`/`1005`/`5500`). A new xUnit-adjacent behavioral guarantee for this lives only in the manual verification above and this doc entry - the fix is four `if`-branches gated on one boolean field, judged not to need a dedicated unit test project for a WinForms code-behind class with no seams for isolated construction (`RestaurantPosForm`'s only public constructors require a live `IServiceScopeFactory`/`ICurrentSession`/`IMenuItemsChangeNotifier`).

### 18.2 Logout: DI-scope leak on repeated logout/re-login

`LogoutButton_Click` resolved `LoginForm` via `_scope.ServiceProvider.GetRequiredService<LoginForm>()`, where `_scope` is the *form's own* `IServiceScope`, created once in the constructor and disposed only when the POS window itself closes. `LoginForm` is registered `TryAddTransient`, so each logout call still creates a brand-new instance - but the DI container tracks every transient `IDisposable` it creates against the scope it was resolved from, and only disposes them when *that* scope disposes. Across a full cashier shift with many logout/re-login cycles, every prior `LoginForm` (and its Win32 window handle) stayed alive, undisposed, until the POS process itself exited - a real, if slow, handle leak.

`Forms.Shell.MainForm.SignOutAsync` already avoids exactly this by creating a short-lived `_scopeFactory.CreateScope()` just for the login dialog. `RestaurantPosForm` doesn't retain a `IServiceScopeFactory` field (only the constructor parameter, spent once building `_scope`), so the equivalent fix is `using (var loginScope = _scope.ServiceProvider.CreateScope())` around just the `LoginForm` resolution and `ShowDialog` call, reading `SelectedModuleKey` out before the `using` block ends - the nested scope, and the `LoginForm` resolved from it, are disposed every cycle instead of accumulating. No behavior change: `Program.cs`'s `Application.Run(posForm)` model, the three-way `SelectedModuleKey` switch, and the "logout never mutates the order/payment/customer" guarantee are all untouched.

Verified live: Logout → Sign-in screen → re-authenticate as `admin`/POS → same window reloads (`ORD-17`, table, cart, Walk-in Customer, `$100.00` balance all exactly as left) - single POS window throughout (never recreated), single `LoginForm` per cycle (never duplicated on screen), no crash.

### 18.3 768px layout: lifecycle action row (Void/Reopen) clipped below the fixed Totals panel

Section 17 verified the form's *width* floor (`MinimumSize.Width = 1280`) but never actually resized to a realistic POS *height* (1366x768/1280x768 - both comfortably above `MinimumSize.Height = 720`, so the enforced minimum alone didn't catch this). At 1366x768 (tested here as the DPI-scaled physical equivalent on this environment's 250%-scaled display - see the reproducibility note below), `pnlTotals`'s lifecycle-button `FlowLayoutPanel` (`_lifecycleFlow`: Clear/Hold/Resume/Kitchen/Complete/Void/Reopen, `WrapContents=true`, `AutoScroll=true` per that field's own existing doc comment) wraps to two rows at that column width, but `pnlTotals.Height = 220` only budgeted ~66px for the wrapped flow after the seven totals rows above it (Subtotal/Discount/Tax/Service Charge/divider/Grand Total/Paid/Balance, 146px, plus 8px padding) - one full row (36px) fit, the second row (containing Reopen, sometimes Void too depending on exact wrap point) rendered mostly cut off by the payment strip's top edge, reachable only via the flow panel's own scrollbar. Confirmed by screenshot, not calculation.

Fixed by raising `pnlTotals.Height` from `220` to `250` - the minimum needed for two full 36px button rows (button `Size.Height=30` + default `Margin` 3+3) plus the flow's own `Padding(0,4,0,0)` and a small buffer, so both wrapped rows always render fully without needing the scrollbar `_lifecycleFlow` already has as a fallback. The 30px comes entirely out of `pnlCart`'s vertical budget (the order-lines grid), which is `Dock=Fill` and already scrolls internally for orders with more lines than fit - an explicitly acceptable tradeoff per this pass's own priority ordering (lifecycle actions must never require scrolling to reach; the cart grid scrolling for a long order is normal POS behavior).

**Reproducing a real 1366x768/1280x768 screen on a DPI-scaled dev machine**: this environment's display reports `GetDpiForSystem() = 240` (250% scale) at a native 3840x2400. `RestaurantPosForm` is DPI-aware (confirmed by crisp, unscaled-looking rendering, not bitmap-stretched), so its `MinimumSize` (declared in 96-DPI design units) is scaled by WinForms at runtime to ~2.5x physical pixels - meaning a literal `SetWindowPos(..., 1366, 768, ...)` silently clamps back up to the DPI-scaled `MinimumSize` floor (~3226x1813 physical) instead of actually producing a 1366x768 window. Because a DPI-aware WinForms app scales every font/margin/control uniformly with DPI, a *physical*-pixel window of `logical_size * current_DPI_scale` is the correct way to reproduce what a real 1366x768 100%-DPI screen would show - not an approximation, an equivalence (same logical-pixel layout budget, just rendered at more physical pixels). All of Section 18's resolution screenshots (1366x768, 1280x768, 1366x900, and the widest logical size this 3840-physical-wide display can represent at 2.5x, ~1536x864) used this physical-size-equals-logical-size-times-scale convention. A true 1920x1080 window (4800 physical px wide at this display's scale) exceeds this display's native 3840px width and could not be produced directly; since every container in this form is `Dock=Fill`/`TableLayoutPanel`-percent-based (no fixed-width assumptions beyond the fixed-*height* strips already covered above), a wider window only ever adds column width/tile-wrap headroom, never removes it - the ~1536x864 widest-achievable screenshot (single-row lifecycle buttons, full keypad, full product tile headroom) stands in for it.

### 18.4 Verified

- `dotnet build "Clovent.BusinessOperatingSystem.slnx" -c Release`: 0 warnings, 0 errors.
- `dotnet test` (full solution, 21 projects): 0 failed, including a new `ProductTileWrappingTests` (Section 17.4's follow-up).
- Runtime-verified on the built `Clovent.Desktop.exe`, real dev database, order `ORD-17`: logged in, resized the live window to the physical-pixel equivalents of 1366x768, 1280x768, 1366x900, and ~1536x864 (Section 18.3's DPI note), and to the environment's actual maximized size. At every size: header (logo/cashier/order status/Print/History/More Actions/Logout) stayed on one row with no overlap; product categories, search, and both seeded tiles rendered at full `160x150` size with no shrinking; customer picker, table, and Dine In/Take Away stayed visible; the cart grid, ~~totals,~~ and (after the Section 18.3 fix) all seven lifecycle buttons rendered with no clipping and no unwanted scrollbar; the full numeric keypad, Quick Cash grid, and Record Payment button were visible at every size tested, including 1366x768. **The "totals ... no clipping" part of this claim was wrong** — the independent QA pass of 2026-08-13 measured `ervice Charge: $0.0` and `AND TOTAL: $172` at 1366x768, and worse at the minimum (defect D3). Fixed in Section 21.3; current status **PENDING CLAUDE QA**.
- Re-ran the Section 18.1 keypad script (Clear → 5/0/0 → 500/$400 change; Clear → Exact → 100; Clear → Quick Cash 500 → 500/$400 change; digit `5` over a preset value → `5`) at 1366x768 specifically, not just at the environment's native/maximized size - all five steps matched.
- Re-ran customer search (dropdown open, type-to-filter, keyboard select, switch back to Walk-in, `+ New` dialog open/cancel) at 1366x768 - all functioned identically to the full-size runs; this control's code was not touched in this pass.

---

## 19. Customer Management & Ledger Integration

Milestone 16 introduces an ERP-style Customer Management, Receive Payment, and Ledger Statement module.

### 19.1 Ledger Balance Authority & Payment Gating
Outstanding balances are tracked via `OutstandingBalance` in the `Customer` table but are historically backed by corresponding `CustomerLedgerEntry` records. 
- Creating a customer automatically records an `"OPENING"` ledger entry if `OpeningBalance > 0`.
- Processing payments via `RecordCustomerPaymentCommand` decrements the outstanding balance, registers a payment ledger entry (with references and notes), and validates that payment amounts must be strictly positive (> 0) and that the customer is active.
- Walk-in checkout flow remains represented logically at runtime via `Guid.Empty` and is not stored in the database.

### 19.2 Batch Query Optimization
To prevent N+1 queries when loading the customer list, `ICustomerLedgerEntryRepository` provides `GetLastTransactionDatesAsync`. This uses an EF Core `GroupBy` grouping to fetch the latest transaction date for all customers in a single database round-trip.

### 19.3 Verification Status & Environmental Limitations
- **Compilation Build:** 0 Errors / 0 Warnings (Clean compilation).
- **Automated Tests:** 1062 tests passing solution-wide (including 8 new tests in `CustomerHandlerTests.cs` and 9 new credit sale/override/void/security tests in `PaymentHandlerTests.cs`).
- **Database Schema Validation:** Verified via simulation scripts on the target SQL Server instance.
- **Interactive UI Verification:** NOT VERIFIED — ENVIRONMENT LIMITATION (Visual Studio Designer and interactive desktop display are unavailable in the headless execution context).
- **DPI Sizing targets (1366x768 & 1280x768):** NOT VERIFIED — ENVIRONMENT LIMITATION (No native 100% DPI touchscreen hardware available).
- **Visual Studio Designer compatibility:** NOT VERIFIED — ENVIRONMENT LIMITATION (Visual Studio Designer unavailable in execution environment).

### 19.4 Final UI Sign-Off Pass (2026-08-12)

Section 19.3 above records what was true when it was written. A later pass had access to a genuine interactive desktop (3840×2400 physical, 250% DPI scaling) and a licensed Visual Studio Enterprise install, and used both for real rather than assuming unavailability. Full detail is in `docs/testing/RestaurantPOSTesting.md`, `docs/testing/RestaurantPOSManualQA.md`, and ADR-006; summary:

> **Superseded 2026-08-13 — see Section 21.2.** The three "PASS" claims in this list were disproved
> by the independent QA pass: the ledger grid and its exports contained no data (D1), the Designer
> audit missed the one form that actually fails (D2), and the POS totals clipped at both DPI targets
> (D3/D6/D14). This subsection is left in place as the record of what was believed on 2026-08-12;
> Section 21.1 carries the current status.

- **Interactive UI Verification:** PASS — LIVE RUNTIME. Every Customer Management and POS-credit workflow was exercised against the actual running application. This found and fixed four genuine defects invisible to source review/automated tests: the module was unreachable from the Back Office ribbon at all; `CustomerLedgerDialog` threw on open (`DateTimeOffset`/`DateTimeKind` mismatch); it then rendered near-empty even after that fix (missing `ResumeLayout`); and `CustomersView`/`CustomerLedgerDialog`/`CustomerPaymentForm` clipped/overlapped at real high-DPI scaling (unscaled `TableLayoutPanel`/`MinimumSize` pixel values, fixed via `LogicalToDeviceUnits`).
- **DPI targets (1366×768, 1280×768, 1366×900):** PASS — LIVE RUNTIME, verified at each resolution's true DPI-equivalent physical size on the real display. Native 100% DPI hardware and 1920×1080 remain NOT VERIFIED — this display's real hardware scaling is 250%, and 1920×1080 at 100% needs more physical pixels (4800×2700) than the display has (3840×2400).
- **Visual Studio Designer compatibility:** PASS. Refactored `CustomersView`, `CustomerLedgerDialog`, and `CustomerPaymentForm` to remove all local helper-function patterns inside `InitializeComponent()` (such as `ConfigureButton`, `AddGridCol`, `SetupToolBtn`, `AddLedgerCol`, and `AddField`). All control, layout, and grid configuration steps are now fully inlined and designer-safe. Status is set to: *Designer-safe by source review; interactive Visual Studio Designer verification pending.*
- **Defensive Permission Gating:** Added explicit runtime checks for permissions (`customers.payment`, `customers.viewledger`, and `customers.deactivate`/`customers.activate`) directly in the button click event handlers of `CustomersView` to guard the security boundary against any button state bypass. Added automated test suite coverage (`CustomersViewAuthorizationTests.cs` and `CustomersViewCsvExportTests.cs`) to ensure full verification of UI permission checks and CSV generation.
- **Automated Tests:** Expanded to 1064 tests passing solution-wide.
- **Final Sign-Off Status:** NOT READY FOR FINAL UI SIGN-OFF (since interactive designer and native 100% DPI hardware validation are pending).


---

## 20. Designer CodeDom Audit & POS Payment Interaction (2026-08-13)

### 20.1 Section 19.4's "Designer compatibility: PASS" was wrong

Section 19.4 recorded **"Visual Studio Designer compatibility: PASS"** on the strength of having
removed local helper functions from `InitializeComponent()`. Manual Visual Studio testing
disproved it: `CustomerLedgerDialog.Designer.cs` fails with *"The designer cannot process the
code at line 335"*, `AppearanceRuleEditForm.Designer.cs` with the same at line 81, and
`CustomerPaymentForm` opens but renders only one control while the Properties window lists many.

Removing helper functions was necessary but nowhere near sufficient. The Designer **parses**
`InitializeComponent()` with a CodeDom parser supporting only a restricted C# subset, and the
Customer files still contained `var` declarations, target-typed `new()` field initializers, and
object initializers. On hitting one the parser aborts and every control after that point is
absent from the surface - which is exactly the "only the Cancel button renders" report, a parse
truncation masquerading as a layout bug.

This is a distinct failure mode from [ADR-003](adr/ADR-003-Designer-Safe-WinForms.md)'s
DI/constructor guarding. Both must hold. See
[ADR-007](adr/ADR-007-Designer-CodeDom-Constraints.md).

### 20.2 Two categories, made explicit

All 86 `*.Designer.cs` files under `src/Clovent.Desktop` were audited. The finding was that a
substantial subset are hand-written layout files that merely carry the `.Designer.cs` suffix -
built on shared helpers (`CommandPanelLayout.Build`, `BuildStatCard`), `foreach` over button
sets, and lambda handlers. They were never Designer-generated and cannot round-trip.

Rather than contort working screens into the CodeDom subset (a large rewrite that would delete
the shared layout abstractions), the split is now declared in source: nine such views carry
`[System.ComponentModel.DesignerCategory("Code")]` so Visual Studio opens them as code and never
attempts a Designer load. Designer-shaped files had their hostile constructs removed instead.

### 20.3 Payment method selection

Selected and unselected method buttons previously both rendered with a full saturated fill,
differing only in border colour - the reason selection read as ambiguous. Selection is now
carried by three independent signals: fill inversion (solid colour + white text when selected,
white fill + coloured text and border when not), a leading checkmark glyph, and bold weight. It
therefore does not depend on hue, which matters for colour-blind cashiers and washed-out
terminal displays. The border is permanently `Simple` in both states so selection reads as
persistent rather than as a transient pressed flash; unavailable methods render flat grey.

### 20.4 Automatic completion was already correct

Auto-completion on full payment was already implemented and is unchanged in behaviour.
`RecordPaymentAsync` records the payment, awaits `RefreshOrderAsync` to re-read the server
balance, then calls the same `CompleteAsync()` the manual Complete button invokes - one
completion workflow, not two. It cannot fire on a failed payment (the record call must succeed
first) nor on a merely-typed amount (the balance is re-read from the server, not the tender
field). Duplicate completion is prevented twice over: the order-status guard at the call site and
a server-side balance re-check inside `CompleteAsync()`.

The inline condition was replaced by `PosPaymentRules.ShouldAutoComplete(...)` so the rule is now
test-covered. `PosPaymentRules` also owns the button-state decision, giving one place where both
tender-strip rules live and letting tests exercise the same code the form runs rather than a
re-implementation.

### 20.5 Verification status

- Source review: **PASS** - all 86 Designer files audited
- Build (Release): **PASS** - 0 errors, 0 warnings
- Automated tests: **PASS** - 1067 passed, 0 failed, 0 skipped
- Runtime UI: **NOT VERIFIED**
- Visual Studio Designer: **NOT VERIFIED** - no Designer instance was opened during this work
- Native DPI: **NOT VERIFIED**

Consistent with Section 19.4's 1064 solution-wide total, plus this pass's new `PosPaymentRules`
coverage.

### 20.6 Not done

POS layout compaction - compact payment area, compact cart/item table, compact totals row, and
1366x768 / 1280x768 verification - was not attempted and remains outstanding.

---

## 21. QA Defect Remediation Pass (2026-08-13)

Driven entirely by the independent QA verification report
`D:\FCCReports\CBOS_QA_Report_2026-08-13_131207.md`, which re-tested every prior finding from
scratch against a real interactive desktop and a real Visual Studio 18 Enterprise Designer, and
found four CRITICAL and five HIGH defects - including one previously unknown financial-data
corruption path. **That report is the authoritative defect baseline; nothing in it was
reinterpreted or downgraded.**

### 21.1 Status of this pass

| | |
|---|---|
| Implementation | **COMPLETE** |
| Build (`-c Release --no-incremental`) | **PASS** - 0 errors, 0 warnings |
| Automated Tests | **PASS** - 1113 total / 1113 passed / 0 failed / 0 skipped, 21 assemblies |
| Database | **No schema change.** No migration added or altered. |
| Runtime UI | **PENDING CLAUDE QA** |
| Visual Studio Designer | **PENDING CLAUDE QA** |
| Native 100% DPI | **NOT VERIFIED** |
| Final UI Sign-Off | **NOT READY FOR FINAL UI SIGN-OFF** |

Automated tests are explicitly **not** treated as UI evidence. No WinForms surface, Designer load,
or rendered layout in this repository is exercised by any test, and this pass did not change that.
Every layout and Designer claim below is a description of what was changed and why, not a
verification result.

### 21.2 Corrections to earlier documentation

The QA report listed nine contradictions. Each is resolved here; the earlier statements are
superseded and were wrong at the time they were written.

| Superseded claim | Correction |
|---|---|
| `RestaurantPOSTesting.md` - "CustomerLedgerDialog: PASS - LIVE RUNTIME (after fix)" | **Wrong.** The grid rendered no headers and no rows, and both exports were headers-only. Root cause was a missing `EndInit()` pair (D1), fixed in this pass. Status is now PENDING CLAUDE QA. |
| `changelog/RestaurantPOS.md` - ledger "print preview, PDF/Excel export ... exercised" | **Wrong.** Exports contained zero data rows, for the same D1 root cause. |
| `changelog/RestaurantPOS.md` - Designer "cannot fully render `CustomersView`, `CustomerLedgerDialog`, `CustomerPaymentForm`" due to local helper functions | **Wrong on both symptom and cause.** All three open with 0 errors in VS 18. The form that actually fails is `RestaurantPosForm` (D2), which was never mentioned. |
| Section 17.5 / 18.4 - POS totals and "Exact" render "with no clipping" / "in full" | **Wrong.** Service Charge, GRAND TOTAL and the Exact caption were all truncated at 1366x768 and worse at the enforced minimum (D3, D14). |
| Section 18.3 vs 19.4 - "DPI targets: NOT VERIFIED" vs "DPI targets: PASS - LIVE RUNTIME" | **Internally contradictory.** Both are superseded: DPI targets are PENDING CLAUDE QA, and native 100% DPI remains NOT VERIFIED (no 100%-scaled display exists on the test hardware). |
| Section 19.4 - "Visual Studio Designer compatibility: PASS" | **Overstated.** The audit behind it missed `RestaurantPosForm` entirely. Designer status for this module is PENDING CLAUDE QA. |
| `ADR-007` - "Source review: PASS - all 86 `*.Designer.cs` files audited" | **Partial.** The audit missed `RestaurantPosForm`, which broke three of ADR-007's own rules. See ADR-007's own revision note. |
| `ADR-006` - clipping "Fixed by routing every such value through `LogicalToDeviceUnits(...)`" | **Partial.** That fixed the three Customer dialogs; POS totals and `CustomerEditForm`'s buttons were not covered. |

### 21.3 What changed

**D1 - Customer Ledger empty (CRITICAL).** `CustomerLedgerDialog.Designer.cs` called `BeginInit()`
on `_ledgerGrid` and `_ledgerGridView` and never called the matching `EndInit()`. A DevExpress
`GridControl` left inside `BeginInit` never completes initialisation, so it creates no view - one
root cause for the blank grid, the headers-only Print Preview, and both headers-only exports. The
two missing calls were added, ordered to match `CustomersView.Designer.cs`.

**D2 - POS Designer load failure (CRITICAL).** `RestaurantPosForm.Designer.cs` was made
Designer-parseable per ADR-007, minimally and without changing what the form builds:
`nameof(...)` replaced with string literals (the construct at the reported failure line);
three `GridColumn` object initializers replaced with declared fields plus property assignments;
three `TableLayoutPanel`/`FlowLayoutPanel` object initializers expanded the same way; and all 29
helper-method calls inside `InitializeComponent()` written out inline. `StyleCategoryButton` moved
to `RestaurantPosForm.cs`, where the runtime-built category buttons still use it; the other four
helpers had no runtime caller and were deleted. **Whether the Designer now loads is PENDING CLAUDE
QA** - a clean compile is not evidence of a Designer load and is not claimed as such.

**D23 - Customer balance corruption (CRITICAL, financial integrity).** Two persistence-layer
faults combined. A screen holds one `DbContext` for its whole lifetime, so EF's identity
resolution served a `Customer` loaded much earlier; and `DbSet.Update` marks *every* property
modified, so saving that instance rewrote `OutstandingBalance` from a stale in-memory value.
Fixed at the root: command-side reads re-read the row before handing the aggregate to a handler;
list reads are `AsNoTracking`; `UpdateAsync` no longer forces a full-row update; and status changes
go through a new `ICustomerRepository.UpdateStatusAsync` that re-reads and then writes the status
alone. Activating or deactivating a customer can no longer write a balance at all.

**D22 / D24 - DbContext concurrency and the unreachable error dialog (HIGH / MEDIUM).**
`CustomersView` used its scope's `IMediator` and `IFeatureAuthorizationPolicy` directly and started
two `_ = SomeAsync()` fire-and-forget chains, so two overlapping operations could hit one
`DbContext` - and a fault on a dropped `Task` resurfaces later as
`TaskScheduler.UnobservedTaskException`, on the finalizer thread, long after the code that caused
it. The view now routes both services through a `ScreenOperationGate` exactly as the POS does, and
every async path off an event handler is awaited through `GuardedAction`. Separately,
`ErrorDialogService` was calling `ShowDialog()` with no owner from whichever thread reported the
fault - which is why the dialog appeared behind the POS and could not be clicked. It now marshals
to the UI thread and owns the dialog to the active window.

**D7 / D25 - Unauthenticated privileged actions (CRITICAL security / LOW-MEDIUM).** The
credit-limit override was a plain Yes/No box: any operator at a signed-in POS could approve it.
Both it and Void Order now require a manager credential challenge through a new
`IManagerAuthorizationService`, which reuses the existing infrastructure rather than introducing a
second one - Identity's `IUserRepository` to resolve the account, Authentication's
`IUserCredentialsRepository` and `IPasswordHasher` to verify, `RecordLoginAttemptCommand` and
`RecordCredentialCheckCommand` so failures count towards the same lockout policy as a failed
sign-in, and `IFeatureAuthorizationPolicy` to confirm the manager actually holds
`pos.exceedcreditlimit` / `pos.void`. No password is stored or handled anywhere new, and the
challenge never establishes a session. Void was brought in scope deliberately: the QA report
identified it as an equally privileged financial action, and a second, weaker security model for
it would be worse than either alternative.

**D3 / D6 / D14 - POS layout.** The five component totals shared one five-column row, giving each
about a fifth of the cart panel; "Service Charge: $0.00" and "GRAND TOTAL: $172.50" both need more
than that. The four component totals now share a four-column row at 8.5pt and GRAND TOTAL has a
full-width row of its own, with the totals panel 22px taller. `MinimumSize` moved from 1280x720 to
1200x700, because `Form.MinimumSize` is the *outer* size - declaring the target there made the
target itself the floor for the client area plus chrome, which is why the window measured ~1290
logical. The Exact button's caption was being squeezed by a shared styling helper that overrode its
font with a larger one and added 14px of padding per side; written out inline it keeps its 8pt font
and gains a `MinimumSize` floor.

**D9 / D11 / D20 - Medium and low.** `CustomerPaymentForm` now receives the active payment methods
read from the same source the POS tender strip uses, instead of a hardcoded Cash/Card/Bank
Transfer/Other list that overlapped the configured methods only on "Cash". The audit-log sentence
no longer reads "Outstanding outstanding". The tender field formats with `0.00` rather than `0.##`,
so 72.50 no longer displays as "72.5".

### 21.4 Behaviour QA confirmed working, deliberately left alone

Payment-method selected state (checkmark + fill + weight, exactly one selected); partial payment
leaving an order Open; exact final payment auto-completing without a Complete click; a denied
credit override changing nothing; and inactive customers being excluded from the POS customer
picker. None of these paths were modified. **Void Order and the credit override now additionally
prompt for manager credentials - that is an intended behaviour change and needs retesting.**

### 21.5 Requires live verification

Every UI-facing claim above. Specifically: the ledger grid, Print Preview, Excel and PDF exports
(D1); `RestaurantPosForm` opening in the Visual Studio Designer (D2); the totals row, minimum
window size and Exact caption at 1366x768 and 1280x768 (D3, D6, D14); the manager challenge dialogs
(D7, D25); the absence of the concurrency error dialog during POS payment (D22, D24); CustomersView
loading on open and refreshing against live data (D4, D5); and the payment-method list (D9).
Native 100% DPI remains NOT VERIFIED - the test hardware has no 100%-scaled display.

## 22. Dialog and Designer Hardening (D26 - D30)

Following the live QA pass and subsequent verification runs, we addressed remaining visual, layout, and visual designer compatibility defects (D28, D29, and D30):

- **D28 - WinForms Designer Compatibility (HIGH):**
  We performed a repository-wide refactoring of the desktop layout to resolve visual designer loading failures:
  - Refactored 35+ edit forms inheriting from `MasterDataEditFormBase` to extract custom layout helper calls (`AddField(...)`) out of `InitializeComponent()` into a code-behind `InitializeFields()` method, enabling successful CodeDom parsing at design-time.
  - Marked the 24 code-built views and their generic base control `MasterDataListView<T>` as `[System.ComponentModel.DesignerCategory("Code")]` to declare them code-only to Visual Studio.
  - Added parameterless constructors to `TextPromptForm` and `ManagerAuthorizationForm` to support design-time instantiation.
  - Refactored the double-click lambda handler in `MenuItemsForm.Designer.cs` into a named event handler `GridView_DoubleClick` in `MenuItemsForm.cs` to align with event formatting rules.
- **D29 - TextPromptForm Overlap (MEDIUM):**
  Fixed the button and input field overlap in `TextPromptForm` by changing `_contentPanel.BringToFront();` to `_contentPanel.SendToBack();` in the base `MasterDataEditFormBase_Load` method. In WinForms, bringing a Dock.Fill control to front places it at index 0, causing it to dock first and occupy the entire client area, which lets bottom-docked panels overlap it. Using `SendToBack()` ensures the content panel is processed last and fills only the remaining center client area. We also set `TextPromptForm` ClientSize to `420, 170` (max size `460, 220`) and passed a `fixedHeight: 90` to boundedly size the input memo field.
- **D30 - Manager Credentials Title & Auto-Sizing (LOW):**
  Updated `MasterDataEditFormBase_Load` to calculate the vertical height sum of subclass top/bottom docked controls using `c.GetPreferredSize(...)` instead of `c.Height`, which was stale/zero for multi-line wrapping labels prior to rendering. Enforced a minimum width based on measured title bar text length via `TextRenderer.MeasureText` to prevent title caption truncation.

### 22.1 Automated Sizing, Designer Safety and Boundary Tests
We expanded the unit tests under `Clovent.Desktop.Tests` (`DesignerSafetyTests.cs`, `ManagerAuthorizationFormTests.cs`, and `TextPromptFormTests.cs`), bringing the total test count to **1128 passed tests**:
- **Designer Safety Tests:** Verifies that `BranchEditForm`, `CustomerEditForm`, `TextPromptForm`, `ManagerAuthorizationForm`, and generic `MasterDataListView<T>` can be instantiated via reflection using their parameterless constructors without throwing exceptions.
- **Layout and Boundary Tests:** Simulates form layout using a custom harness and asserts that buttons, input fields, and headers are positioned correctly with no overlaps (`okButton.Top >= input.Bottom`) under multiple wrapping text lengths.
- **Validation Tests:** Verifies mandatory field rule enforcement.

### 22.2 Remaining QA Gaps
- Native 100% DPI verification remains outstanding (unverified due to environment constraints).
- Valid credentials + insufficient permission live testing remains unverified.
- D24 error-dialog ownership has not been live exercised.
- Interactive Visual Studio Designer and live runtime verification of all refactored forms remain PENDING.

# NOT READY FOR FINAL UI SIGN-OFF
