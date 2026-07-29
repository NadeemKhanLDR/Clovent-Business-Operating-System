---
title: Restaurant POS Architecture Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Milestone 15
applies_to: src/Clovent.Restaurant, src/Clovent.Restaurant.Application, src/Clovent.Restaurant.Infrastructure, src/Clovent.Desktop
---

# Restaurant POS Architecture Reference

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
4. **No screen exists yet to manage `Discount`/`ServiceCharge` as standalone reference data** (e.g. a fixed list of standard discount reasons/percentages a cashier picks from) - every one is entered free-form via `DiscountDialog`/`ServiceChargeDialog` at apply-time. Acceptable for this milestone's scope; flagged in case a future requirement wants a managed catalog of standard discounts/charges.
