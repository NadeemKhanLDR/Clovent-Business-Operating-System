---
title: Desktop Administration Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 15
applies_to: src/Clovent.Desktop
---

# Desktop Administration Reference

Milestone 13 ("Organization & Master Data Foundation") adds nine management screens to `Clovent.Desktop` - the first business-data CRUD screens in the solution (Milestones 7-12 built the host shell, login, authentication, authorization, and dashboard, but no screen that lists/creates/edits a domain entity). This document covers the shared screen infrastructure those nine screens are built on, how authorization gates them, and how the Dashboard was extended to show real business context.

---

## 1. Shared infrastructure: one grid+toolbar pattern, not nine

Every list screen (Organization, Company, Branch, Department, Warehouse, Terminal, Fiscal Year, Currency) is built on `MasterDataListView<TDto>` (`Clovent.Desktop.MasterData`) - a generic `XtraUserControl` wrapping a `DevExpress.XtraGrid.GridControl`, a search box, and New/Edit/Activate/Deactivate/Refresh buttons. A screen supplies columns (`MasterDataColumn` records: field name + caption, bound via `GridView.Columns.AddVisible` - reflection-based binding onto the DTO record's public properties) and delegate properties (`LoadItemsAsync`, `SearchTextSelector`, `StatusSelector`, `CanUseFeatureAsync`, `OnNew`/`OnEdit`/`OnActivate`/`OnDeactivate`) rather than each screen re-implementing grid wiring, search filtering, or button-enablement logic nine times.

**Business Settings is the one screen that isn't a grid.** `BusinessSettingsManagementView` is a single-record form (one record exists per organization - see `MasterData.md` Section 2) with combo-box lookups into the Currency/Language/TimeZoneEntry/FiscalYear catalogs and a Save button, not a `MasterDataListView`. Building it on the shared grid control would have forced an artificial "list of one" onto something that is never a list.

**`OrganizationHierarchySelector`** is the second piece of shared infrastructure: a cascading Organization → Company → Branch combo-box picker. Only the combos a screen actually needs are shown (Company screen needs only the Organization combo to filter by; Branch needs Organization+Company; Department/Warehouse/Terminal need all three) - the unused combos stay hidden and their `Selected*Id` properties stay `null`, rather than every screen carrying dead UI for levels it doesn't use.

**`MasterDataEditFormBase`** is the shared create/edit dialog chrome: a two-column label+editor layout plus OK/Cancel buttons and a `ValidateFields` hook a subclass overrides. Each entity's own edit form (`OrganizationEditForm`, `WarehouseEditForm`, etc.) supplies only its own fields - the dialog shell (sizing, button wiring, validation-failure message box) is written once.

**Pure logic extracted for testability, the same discipline `NavigationMenuBuilder` established in Milestone 11.** `MasterDataFilter` (`Clovent.Desktop.MasterData`) holds the search-filter predicate and the three button-enablement gates (`CanEdit`/`CanActivate`/`CanDeactivate` - each a pure function of "is a row focused," "is the operation permitted," "does the row's status allow it," "does the screen even wire a handler for this operation") as static methods with no DevExpress dependency, so they can be unit tested without a Windows Forms message loop. `MasterDataListView<TDto>` calls into these rather than inlining the logic - see `Clovent.Desktop.Tests/MasterData/MasterDataFilterTests.cs`.

---

## 2. Why some screens have fewer buttons than others

Every screen's button set traces directly back to what its entity's Application-layer command surface actually offers (`MasterData.md` Section 3, `OrganizationArchitecture.md` Section 3) - never a button wired to a command that doesn't exist:

- **Currency** has no Edit button - `Currency` has no rename/update command (the domain itself has no method for it).
- **Warehouse/Terminal** disable their Code field when editing (`WarehouseEditForm`/`TerminalEditForm`'s `isNew` parameter) - `Code` is immutable after creation, so the field is shown (for context) but not editable.
- **Fiscal Year** has no Activate button at all - closing is one-way (`FiscalYear.Close()`, no `Reopen()`), so `MasterDataListView.OnActivate` is deliberately left unset and the Deactivate button is relabelled "Close" (`DeactivateButtonText` property) via `StatusSelector` mapping `"Open"` → `"Active"` / `"Closed"` → `"Inactive"` so the shared enablement logic (which only understands Active/Inactive) still works correctly without duplicating it for a third status vocabulary.
- **Fiscal Year/Business Settings' date and period fields** are immutable after creation for the same domain reason Warehouse/Terminal's Code is.

---

## 3. Authorization: reusing Milestone 10's mechanism, not inventing a new one

**Navigation gating required no new code.** `NavigationMenuBuilder`/`IMenuAuthorizationPolicy` (Milestone 11) already filters *any* registered navigation key generically - registering the nine new keys (`organizations`, `companies`, `branches`, `departments`, `warehouses`, `terminals`, `fiscalyears`, `currencies`, `businesssettings`) via `INavigationService.Register` in `Program.cs` was sufficient; the existing Shell accordion menu automatically hides any key the signed-in user lacks `menu.{key}` for.

**Feature (button-level) gating is per-screen, via the existing `IFeatureAuthorizationPolicy`.** Each screen's constructor wires `CanUseFeatureAsync` to check `feature.{entity}.{operation}` (e.g. `feature.organizations.create`, `feature.currencies.activate`) for the signed-in user, exactly the `feature.{code}` convention `Authorization.md` established in Milestone 10 - no new authorization primitive was introduced, only new permission code strings following the existing convention.

**Resolved Authorization.md's open question #3.** That document flagged: *"No seed data for permissions/roles/policies exists... every `IAuthorizationService` check against [the seed admin user] currently returns empty/false."* This was still true through Milestone 12 and would have made every Milestone 13 screen invisible and every button disabled for the demo admin. `DevelopmentAuthorizationSeedStartupTask` (new, gated by the existing `DesktopOptions.SeedDevelopmentUser` flag, guarded against re-running by checking the admin user already has a role) now seeds every `menu.*`/`feature.*.*` permission code this milestone's screens check, plus one "Administrator" role holding all of them, assigned to the seeded admin user - see `Authorization.md`'s updated Section 4 for the resolution note.

---

## 4. Dashboard: Current Organization/Company/Branch/Fiscal Year/User

`DashboardView` (Milestone 12) gained a new "Business Context" row of five stat cards above the existing Active Sessions/Logins/Notifications row. **Since no tenant-switcher UI exists yet**, "current" resolves to the first Organization in the system (in practice, the one `DevelopmentMasterDataSeedStartupTask` seeds) and its first Company/Branch - the same honest simplification already applied to `IRecentItemsService`'s Company/Branch combo boxes in Milestone 12, not a fabricated "current selection" concept. Current Fiscal Year is read from that organization's `BusinessSettings.DefaultFiscalYearId` (falling back to "Not set"/"Not configured" when no settings or no default fiscal year exist yet) - reusing the exact Application-layer queries the Fiscal Year and Business Settings screens already call, no new backend surface needed for the Dashboard tiles.

---

## 5. DevExpress API verification discipline, continued

Before writing `MasterDataListView`, `GridControl`/`GridView`/`GridColumnCollection`'s public API was verified via a scratch reflection probe (the same discipline Milestone 8's `LoginForm` and Milestone 11's `ShellForm` established) - notably confirming `GridColumnCollection.AddVisible(fieldName, caption)`'s exact overload, that `GridView.OptionsBehavior`/`OptionsView`/`OptionsSelection` are declared on the `ColumnView` base class rather than `GridView` itself, and that `GridView.GetFocusedRow()` returns the bound row object directly (safe to cast to the DTO type), before any of it was used in the shared control.

**A second WinForms-analyzer-driven fix, not a DevExpress one.** Public settable delegate-typed properties (`Func<...>`) on any `Control` subclass trip the WinForms designer-serialization analyzer (`WFO1000`) by default. Every such property on `MasterDataListView<TDto>` (`LoadItemsAsync`, `OnNew`, `OnEdit`, etc.) is annotated `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]` - the standard WinForms fix for a property no designer ever needs to serialize, since this project has no interactive designer surface to serialize into anyway (see `DesktopBootstrap.md`'s original "no `.Designer.cs`" reasoning).

---

## 6. Verified: builds clean, tests pass

- `Clovent.Desktop`: 0 build warnings, 0 errors, all nine screens + shared infrastructure + Dashboard changes + two new seed tasks.
- `Clovent.Desktop.Tests`: 58 tests (up from 37 pre-Milestone-13 - the delta is `MasterDataFilterTests`, 21 new tests covering the extracted pure filter/enablement logic).

---

## 7. Open questions for Solution Architect review

1. **`OrganizationHierarchySelector` re-queries its combo levels on every parent selection change**, with no caching - acceptable at this data scale (a demo/early-stage tenant list), but worth revisiting if a production tenant ends up with hundreds of companies/branches and the cascading re-fetch becomes a perceptible delay.
2. **"Current Organization/Company/Branch" on the Dashboard is "the first one," not a real selection** (Section 4) - flagged the same way `Dashboard.md`'s own open question #2 already flags the absence of a canonical "long-lived Scoped-dependency" pattern. **Needs a decision**: does a future milestone need an actual tenant/context switcher UI, and if so, where does "current context" get persisted (`ICurrentSession`, a new `ICurrentBusinessContext`, or configuration)?
3. **No pagination exists on any of the nine grids** - every screen loads its full result set via `LoadItemsAsync` and filters client-side. Acceptable at seed-data scale; flagged in case a future requirement needs server-side paging once a real tenant's Warehouse/Terminal/Department counts grow.

---

## 8. Milestone 14 addendum: eleven more screens, on the same shared infrastructure

Milestone 14 ("Product Catalog & Inventory Foundation") adds eleven more management screens (Product, Category, Brand, Unit, Barcode, Variant, Price, Warehouse Stock, Stock Adjustment, Stock Transfer, Inventory Transactions) - all still built on `MasterDataListView<TDto>`, none needing a bespoke grid. Full detail, including the three additive extensions made to the shared control (extra action buttons, CSV export/import, two new picker controls) and each screen's own design decisions, lives in the new `WarehouseManagement.md` rather than duplicated here, following this document's own precedent of covering one milestone's screens per addendum section rather than rewriting Sections 1-7 to describe two milestones' screens at once.

`DevelopmentAuthorizationSeedStartupTask` (Section 3) was extended with this milestone's own `menu.*`/`feature.*.*` permission codes, following the identical convention - no new authorization primitive was needed, the same "reusing Milestone 10's mechanism" story as Section 3 already tells for Milestone 13's nine screens.

---

## 9. Milestone 15 addendum: ten more screens/dialogs, one additive extension, one genuinely new pattern

Milestone 15 ("Restaurant POS Core") adds ten more screens/dialogs (Dining Area, Table, POS, Running Orders, Hold Orders, Payment, Bill Split, Table Transfer, Merge Tables, Kitchen Ticket Viewer). Dining Area/Table/Running/Hold Orders/Kitchen Ticket Viewer stay on `MasterDataListView<TDto>` unchanged; full detail lives in the new `RestaurantPOSArchitecture.md` rather than duplicated here, following this document's own precedent (Section 8) of one addendum section per milestone's screens.

**One additive extension to shared infrastructure**: `EntityPicker.SelectId(Guid?)` (`RestaurantPOSArchitecture.md` Section 9) - a screen can now restore a specific selection after reloading a picker's options, without disturbing `LoadItems`'s existing always-select-first behavior every prior consumer (Variant, WarehouseStock, StockAdjustment, StockTransfer, Inventory Transactions scoping) still relies on.

**`RestaurantPosView` and `PaymentForm` are the first screens in this solution built on no prior template** - every earlier screen is either a single-entity CRUD grid (`MasterDataListView<TDto>`) or a single-record form (`BusinessSettingsManagementView`, Section 1). A POS screen (header-plus-lines transaction, composed picker/grid/action-rail layout, one running `OrderDto?` refreshed as a whole after every action) and a stateful multi-action dialog (`PaymentForm`, performing its own `IMediator` calls and re-querying its own running balance rather than a single OK/Cancel round trip) are both genuinely new UI shapes, documented in full in `RestaurantPOSArchitecture.md` Section 9 rather than here, since neither generalizes into reusable shared infrastructure the way `MasterDataListView`/`MasterDataEditFormBase` did.
